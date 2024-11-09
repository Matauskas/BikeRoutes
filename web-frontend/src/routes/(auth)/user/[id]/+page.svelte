<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import {
		type User,
		type Route,
		type Trip,
		listUserRoutes,
		loggedInUserId,
		getUser
	} from '$lib/store';
	import { onMount } from 'svelte';
	import CenterColumnLayout from '$lib/components/CenterColumnLayout.svelte';
	import RouteCardUser from '$lib/components/RouteCardUser.svelte';
	import LogoutButton from '$lib/components/LogoutButton.svelte';
	import ProfileInfo from '$lib/components/ProfileInfo.svelte';

	let isLoggedInUser = true;
	let canBeFriended = true;

	const userId: string = $page.params.id;
	let loading = true;
	let user: User;
	let routes: Route[];
	let lastTrip: Trip | undefined;

	onMount(async () => {
		const loggedInUser = await getUser(userId);
		console.assert(loggedInUser !== undefined);
		user = loggedInUser as User;
		isLoggedInUser = userId == String($loggedInUserId);
		canBeFriended = !isLoggedInUser;

		routes = await listUserRoutes(userId);

		loading = false;
	});

	function sendFriendRequest() {
		console.log('// TODO: implement send friend request');
	}
</script>

{#if !loading}
	<CenterColumnLayout>
		<LogoutButton slot="left-column" show={isLoggedInUser} />

		<div slot="right-column" class="flex flex-col gap-4 md:flex-row">
			<button
				class="
				box-shadow-black-3 rounded-l-lg bg-slate-50 p-3
				md:rounded-b-lg md:rounded-tl-none
			"
				class:hidden={!isLoggedInUser}
				on:click={() => goto(`/user/${userId}/edit`)}
			>
				<i class="fa-solid fa-pen fa-lg" />
			</button>

			<button
				class="
				box-shadow-green-3 pr-100 relative w-12 rounded-l-lg bg-green-300 p-3
				md:rounded-b-lg md:rounded-tl-none
			"
				class:hidden={!canBeFriended}
				on:click={sendFriendRequest}
			>
				<i class="fa-regular fa-user fa-lg" />
				<i class="fa-solid fa-plus absolute right-2 top-1" />
			</button>
		</div>

		<ProfileInfo {user} />

		{#if lastTrip}
			<div class="mt-8">
				<div class="mb-4 text-2xl">Paskutinė kelionė</div>
				<a
					class="box-shadow-black-3 flex h-20 w-full flex-row items-center justify-between rounded-lg bg-slate-50 text-sm sm:text-base"
					href={`/trips/${lastTrip.id}`}
				>
					<div class="bg-stripped hidden h-full w-40 rounded-l-lg sm:block" />
					<div class="ml-3 flex flex-row gap-3 sm:ml-6">
						<div>
							<div class="text-center font-semibold">{lastTrip.distance}km</div>
							<div>Nuvažiuota</div>
						</div>
						<div>
							<div class="text-center font-semibold">{lastTrip.duration}h</div>
							<div>Laikas</div>
						</div>
						<div>
							<div class="text-center font-semibold">{lastTrip.participants.length - 1}</div>
							<div>Draugai</div>
						</div>
					</div>
					<div
						class="ml-auto grid auto-cols-min grid-cols-[1fr_auto] grid-rows-2 gap-x-2 pr-3 sm:pr-6"
					>
						<div>Baigta</div>
						<div class="font-semibold">2024-04-5 15:32</div>
						<div>Pradėta</div>
						<div class="font-semibold">2024-04-5 15:32</div>
					</div>
				</a>
			</div>
		{/if}

		{#if routes.length > 0}
			<div class="mt-8">
				<div class="mb-4 text-2xl">Sukurti maršrutai ({routes.length})</div>
				<div class="flex flex-col gap-3">
					{#each routes as route}
						<RouteCardUser {route} />
					{/each}
				</div>
			</div>
		{/if}
	</CenterColumnLayout>
{/if}
