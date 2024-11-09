<script lang="ts">
	import { page } from '$app/stores';
	import CenterColumnLayout from '$lib/components/CenterColumnLayout.svelte';
	import { incomingFriendRequests } from '$lib/store';

	let tabs = [];
	$: tabs = [
		{
			href: '/friends/mine',
			routeId: '/(auth)/friends/mine',
			title: 'Mano'
		},
		{
			href: '/friends/search',
			routeId: '/(auth)/friends/search',
			title: 'Ieškoti'
		},
		{
			href: '/friends/requests',
			routeId: '/(auth)/friends/requests',
			title: 'Pakvietimai',
			showRequestsBubble: $incomingFriendRequests.length > 0
		}
	];
</script>

<CenterColumnLayout>
	<div class="mb-4 mt-6 flex flex-row gap-4 border-b-[3px] border-slate-300 px-1">
		{#each tabs as tab}
			<a
				href={tab.href}
				class="box-shadow-black-3 relative rounded-t-lg px-2 py-1"
				class:bg-slate-50={$page.route.id === tab.routeId}
			>
				{tab.title}
				{#if tab.showRequestsBubble}
					<i class="fa-solid fa-circle fa-xs absolute -right-2 top-0 text-red-400" />
				{/if}
			</a>
		{/each}
	</div>

	<slot />
</CenterColumnLayout>
