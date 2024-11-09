<script lang="ts">
	import { onMount } from 'svelte';
	import { get } from 'svelte/store';
	import { goto } from '$app/navigation';
	import type { Route } from '$lib/store';
	import { loggedInUserId } from '$lib/store';
	import * as api from '$lib/api';
	import Button from './Button.svelte';
	import { page } from '$app/stores';

	export let route: Route;

	let showPopup = false;
	let hideButton = false;

	$: {
		const pageUserId = Number($page.params.id);
		const currentUserId = get(loggedInUserId);
		if (currentUserId === pageUserId) {
			hideButton = true;
		}
	}

	onMount(async () => {
		const userId = get(loggedInUserId);
		const pageUserId = Number($page.params.id);
		let isRouteDuplicate = false;
		//	let isFriend = false;

		if (userId) {
			const response = await api.listRoutes();
			if (response.ok) {
				const routes: Route[] = response.body;
				isRouteDuplicate = routes.some(
					(r: Route) => r.ownerId === userId && r.title === route.title
				);
			}
			///IF U DONT WANT TO SAVE FRIENDS ROUTE
			/*
			const friendsResponse = await api.listFriends();
			if (friendsResponse.ok) {
				const friends = friendsResponse.body;
				for (const friend of friends) {
					console.log(`Friend object:`, friend); 
					console.log(`Checking friend (${friend}) against pageUserId (${pageUserId})`);
					if (friend === pageUserId) {
						isFriend = true;
						break;
					}
				}
			} else {
				console.error('Failed to fetch friends');
			}
            */

			hideButton = isRouteDuplicate;
		}
	});

	function round1(x: number): number {
		return Math.round(x * 10) / 10;
	}

	function togglePopup(event: Event) {
		event.preventDefault();
		showPopup = !showPopup;
	}

	async function saveRoute() {
		const userId = get(loggedInUserId);
		console.log('Logged-in User ID:', userId);

		if (userId) {
			const updatedRoute = { ...route, ownerId: userId };

			const response = await api.saveRoute(updatedRoute);
			if (response.ok) {
				console.log('Route saved successfully');
				goto(`/user/${userId}`);
			} else {
				console.error('Failed to save route');
			}
		}

		showPopup = false;
	}
</script>

<a
	class="box-shadow-black-3 flex h-20 w-full flex-row items-center justify-between rounded-lg bg-slate-50 text-sm sm:text-base"
	href={`/routes/${route.id}`}
>
	<div class="bg-stripped hidden h-full w-40 rounded-l-lg sm:block" />
	<div class="ml-3 flex-grow text-base sm:ml-6 sm:text-lg md:text-xl">{route.title}</div>
	<div class="flex flex-row gap-3 pr-3 sm:pr-6">
		<div>
			<div class="text-center font-semibold">{round1(route.distance)}km</div>
			<div>Atstumas</div>
		</div>
		<div>
			<div class="text-center font-semibold">{round1(route.time)}h</div>
			<div>Laikas</div>
		</div>
	</div>
	{#if !hideButton}
		<Button size="sm" variant="green" class="custom-button" on:click={togglePopup}>
			<i class="fa-solid fa-plus" />
		</Button>
	{/if}
</a>

{#if showPopup}
	<div class="fixed inset-0 flex items-center justify-center bg-gray-800 bg-opacity-75">
		<div class="rounded-lg bg-white p-6 shadow-lg">
			<p>Ar norite išsaugoti šį mašrutą?</p>
			<div class="mt-4 flex justify-end">
				<button
					class="mr-2 rounded bg-red-500 px-4 py-2 text-white hover:bg-red-700"
					on:click={togglePopup}
				>
					Atšaukti
				</button>
				<button
					class="rounded bg-green-500 px-4 py-2 text-white hover:bg-green-700"
					on:click={saveRoute}
				>
					Išsaugoti
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	.custom-button {
		margin-left: -10px;
	}
</style>
