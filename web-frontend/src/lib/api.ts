import Cookies from 'js-cookie'
import type { Id, User, UserChanges, Point } from './store'

export interface RegisterOptions {
    username: string
    password: string
    email: string
    firstName: string
    lastName: string
}

export interface CreateRouteOptions {
	title: string
	points: Point[]
	distance: number
	time: number
}
export interface CreateReviewOptions {
	routeId: number
	rating: number
	description: string
}

export type UpdateRouteOptions = Partial<CreateRouteOptions>

export interface APIResult<ResponseBody = any> {
	ok: boolean
	status: number
	body?: ResponseBody
}

export function containsError(response: APIResult, reason: string, field?: string) {
	if (response.ok) {
		return false
	}

	const errors = response.body
	if (!Array.isArray(errors)) {
		return false
	}

	return errors.some((err) => err.field === field && err.reason === reason);
}

export async function apiFetch(method: string, url: string, jsonBody: object|undefined = undefined): Promise<APIResult> {
	let body = undefined
	let headers: Record<string, string> = {}

	if (jsonBody) {
		headers['Content-Type'] = 'application/json'
		body = JSON.stringify(jsonBody)
	}

	const response = await fetch(url, { method, headers, body })

	let responseBody = undefined
	try {
		responseBody = await response.json()
	} catch (e) {
		// If the response is not JSON encoded, well whatever. Deal with it, you ain't getting it.
	}

	return {
		ok: response.ok,
		status: response.status,
		body: responseBody
	}
}

export function isServerError(status: number) {
	return Math.floor(status / 100) === 5
}

export function isClientError(status: number) {
	return Math.floor(status / 100) === 4
}

export async function login(username: string, password: string) {
    return await apiFetch('POST', '/api/Users/login', { username, password })
}

export async function check_email(email: string) {
	return await apiFetch('POST', '/api/Users/check_email', { email })
}
export async function reset_password(password: string, token: string) {
	return await apiFetch('POST', '/api/Users/reset_password', { password, token })
}

export function logout() {
	Cookies.remove('token')
}

export async function getLoggedInUser() {
	return await apiFetch('POST', '/api/Users/getData')
}

export async function getUser(idOrUsername: string|Id) {
	return await apiFetch('GET', `/api/Users/${idOrUsername}`)
}

export async function saveRoute(options: CreateRouteOptions)
{
	return await apiFetch('POST', '/api/Routes/save', options)
}

export async function updateRoute(id: Id, options: UpdateRouteOptions)
{
	return await apiFetch('PUT', `/api/Routes/${id}`, options)
}

export async function listRoutes() {
	return await apiFetch('GET', '/api/Routes')
}

export async function listUserRoutes(idOrUsername: string|Id) {
	return await apiFetch('GET', `/api/Users/${idOrUsername}/routes`)
}

export async function register(options: RegisterOptions) {
	return await apiFetch('POST', '/api/Users/register', options)
}

export async function updateUserPassword(password: string, newPassword: string) {
	return await apiFetch('POST', '/api/Users/changePsw', { password, newPassword })
}

export async function listUsers() {
	return await apiFetch('GET', '/api/Users/')
}

export async function deleteUser() {
	return await apiFetch('POST', '/api/Users/deleteUser')
}
export async function uploadImage(file: File): Promise<APIResult<string>> {
    const formData = new FormData();
    formData.append('file', file);

    return await apiFetch('POST', '/api/Images/upload', formData);
}

// TODO: temporary function, because updating user is not implemented
export async function updateUser(id: number, changes: UserChanges): Promise<APIResult> {
	return {
		ok: true,
		status: 200,
		body: {
			email: changes.email || "Emailas@gmail.com",
			photoUrl: changes.photoUrl || "https://www.gravatar.com/avatar/00000000000000000000000000000000?d=mp",
			firstName: "admin",
			id: 1,
			lastName: "admin",
			username: "admin"
		}
	}
}

export async function sendFriendRequest(id: Id) {
	return await apiFetch('POST', '/api/friend-requests/', { id })
}

export async function listIncomingFriendRequests() {
	return await apiFetch('GET', '/api/friend-requests/incoming')
}

export async function listOutgoingFriendRequests() {
	return await apiFetch('GET', '/api/friend-requests/outgoing')
}

export async function listFriends() {
	return await apiFetch('GET', '/api/friends')
}

export async function removeFriend(id: number) {
	return await apiFetch('DELETE', `/api/friends/${id}`)
}

export function getUserToken() {
	return Cookies.get('token');
}

export function isLoggedIn() {
	return getUserToken() !== undefined
}
export async function createReview(options: CreateReviewOptions) {
	return await apiFetch('POST', '/api/Reviews/createReview', options)
}
export async function listReviews(routeId: number) {
    return await apiFetch('GET', `/api/Reviews/${routeId}`);
}