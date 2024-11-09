import { writable, get, type Writable } from "svelte/store";
import type { APIResult } from "./api";
import * as api from "./api"
import { page } from "$app/stores";
import { goto } from "$app/navigation";

export type Id = number

export interface Point {
    latitude: number
    longtitude: number
}

export interface User {
    id: Id
    firstName: string
    lastName: string
    username: string
    email: string
    photoUrl?: string
    totalDistance: number
    totalTime: number
    totalCount: number
}

export interface Route {
	route: any;
	[x: string]: any;
	id: Id
	ownerId: number
    title: string
    points: Point[]
    distance: number,
    time: number
}

export interface Trip {
    id: Id,
    distance: number,
    duration: number,
    participants: Id[]
    // TODO: implement trip
}

export interface UserChanges {
    username?: string
    email?: string
    firstName?: string
    lastName?: string
    photoUrl?: string
}

export let saveRouteData = writable({})
export let users = writable<User[]>([])
export let friends = writable<Id[]>([])
export let incomingFriendRequests = writable<Id[]>([])
export let outgoingFriendRequests = writable<Id[]>([])
export let routes = writable<Route[]>([])
export let loggedInUserId = writable<number|undefined>()

export function addOrUpdateEntityById<T extends { id: number }>(entities: Writable<T[]>, newEntity: T) {
    entities.update(entities => {
        const existingEntity = entities.find(entity => entity.id === newEntity.id)
        if (existingEntity) {
            Object.assign(existingEntity, newEntity)
        } else {
            entities.push(newEntity)
        }
        return entities
    })
}

export async function getUser(idOrUsername: Id|string): Promise<User | undefined> {
    let foundUser = get(users).find(user => user.id === idOrUsername || user.username === idOrUsername)
    if (foundUser === undefined) {
        const userResult = await api.getUser(idOrUsername)
        if (userResult.ok) {
            foundUser = userResult.body
            addOrUpdateEntityById(users, foundUser as User)
        } else {
            // TODO: Show error message? idk
            //
            // Need to think about edge-case, when a user is deleted and we would need to remove
            // the user from out store.
            // VS.
            // The user just doesn't exist, so nothing needs to be done.
            //
            // Maybe just always remove that user?
        }
    }

    return foundUser
}

export async function getRoute(id: Id): Promise<Route | undefined> {
    // TODO: Perform API requesst, if route is not found locally
    return get(routes).find(r => r.id == id)
}

export async function updateUser(id: number, changes: UserChanges) {
    const response = await api.updateUser(id, changes)
    if (response.ok) {
        addOrUpdateEntityById(users, response.body)
    }

    return response
}

export async function getLoggedInUser(): Promise<User | undefined> {
    const userId = get(loggedInUserId)
    if (!userId) return undefined;

    return await getUser(userId)
}

export async function login(username: string, password: string): Promise<APIResult> {
    const response = await api.login(username, password)

    if (response.ok) {
        const user = response.body
        loggedInUserId.set(user.Id)
        addOrUpdateEntityById(users, user)
    }

    return response
}

export async function logout() {
    api.logout()
    loggedInUserId.set(undefined)
    await goto('/');
}

export async function listUserRoutes(idOrUsername: Id|string): Promise<Route[]> {
    const response = await api.listUserRoutes(idOrUsername)
    if (!response.ok) {
        throw new Error("Failed to load user routes")
    }
    const userRoutes = response.body as Route[]

    for (const route of userRoutes) {
        addOrUpdateEntityById(routes, route)
    }

    return response.body as Route[]
}

export async function removeFriend(friendId: Id): Promise<void> {
    const result = await api.removeFriend(friendId)
    if (!result.ok) {
        // TODO: Show error message
        return
    }

    friends.update(friends => {
        return friends.filter(friend => friend !== friendId)
    })
}

// TODO: This function should not be public, the user `store.ts` should not care about "refreshing" the friends list
export async function refreshFriends(): Promise<void> {
    const friendUsernames = await api.listFriends()
    if (friendUsernames.ok) {
        const friendIds: Id[] = []

        for (const username of friendUsernames.body) {
            const user = await getUser(username)
            if (!user) {
                // TODO: Show error message?
                continue
            }

            friendIds.push(user.id)
        }

        friends.update(() => friendIds)
    } else {
        // TODO: Show error message
    }
}

export async function setup(): Promise<void> {
    {
        const response = await api.getLoggedInUser()
        if (response.ok) {
            loggedInUserId.set(response.body.id)
            addOrUpdateEntityById(users, response.body as User)
        } else {
            api.logout()
            return
        }
    }

    await refreshFriends();

    {
        const response = await api.listRoutes()
        if (!response.ok) {
            throw new Error("Failed to load user routes")
        }

        for (const route of (response.body as Route[])) {
            addOrUpdateEntityById(routes, route)
        }
    }

    {
        const response = await api.listIncomingFriendRequests();
        if (!response.ok) {
            throw new Error("Failed to load friend requests")
        }

        incomingFriendRequests.set(response.body)
    }

    {
        const response = await api.listOutgoingFriendRequests();
        if (!response.ok) {
            throw new Error("Failed to load friend requests")
        }

        outgoingFriendRequests.set(response.body)
    }
}