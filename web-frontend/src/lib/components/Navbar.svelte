<script lang="ts">
	import { loggedInUserId, incomingFriendRequests } from '$lib/store';

	export let showBackButton = false;

	let highlightFriends = false;
	$: highlightFriends = $incomingFriendRequests.length > 0; // Controls the red bubble on the friends button
</script>

<div
	class={`
			box-shadow-black-3
			z-10 flex flex-row bg-slate-50 font-bold
            md:max-w-min md:rounded-b-lg
            ${$$props.class || ''}
		`}
>
	<a class="w-full border-r-2 border-r-slate-200 p-3 pt-4" href="/routes">
		<div class={`${showBackButton ? 'hidden md:flex' : 'flex'} flex-col items-center`}>
			<i class="fa-solid fa-route block" />
			Maršrutai
		</div>
	</a>
	<a
		class="
			hidden w-full flex-col items-center border-r-2 border-r-slate-200 p-2 pt-4
			md:flex
		"
		href="/map"
	>
		<i class="fa-regular fa-map block" />
		Žemėlapis
	</a>
	<a
		class={`${showBackButton ? 'hidden md:flex' : 'flex'} w-full flex-col items-center border-r-2 border-r-slate-200 p-3 pt-4`}
		href="/friends/mine"
	>
		<div class="fa-stack relative block h-4 w-7">
			<i class="fa-regular fa-user absolute right-0 stroke-2" />
			<i class="fa-regular fa-user absolute" />
			<i
				class="fa-solid fa-circle fa-2xs absolute -right-2 -top-1 text-red-400"
				class:hidden={!highlightFriends}
			/>
		</div>
		Draugai
	</a>
	{#if showBackButton}
		<button
			class="flex w-full flex-col items-center border-r-2 border-r-slate-200 p-3 pt-4 md:hidden"
			on:click={() => history.back()}
		>
			<i class="fa-solid fa-arrow-left block" />
			Atgal
		</button>
	{/if}
	<a class="w-full p-3 pt-4" href={`/user/${$loggedInUserId}`}>
		<div class={`${showBackButton ? 'hidden md:flex' : 'flex'} flex-col items-center`}>
			<i class="fa-regular fa-user block" />
			Profilis
		</div>
	</a>
</div>
