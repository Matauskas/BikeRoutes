<script lang="ts">
	import {
		getUser,
		incomingFriendRequests,
		outgoingFriendRequests,
		refreshFriends,
		type Id,
		type User
	} from '$lib/store';
	import * as api from '$lib/api';

	export let friendId: Id;

	let friend: User | undefined;
	$: getUser(friendId).then((user) => {
		friend = user;
	});
</script>

<button
	{...$$restProps}
	on:click
	class="box-shadow-black-3 flex flex-row items-center gap-2 rounded-lg bg-slate-50 px-3 py-2 text-lg md:px-4 md:py-3 md:text-xl"
>
	{#if friend}
		<div class="flex size-10 items-center justify-center rounded-full bg-slate-300 md:size-14">
			<div class="fa-regular fa-face-grin-squint text-3xl text-slate-500 md:text-4xl" />
		</div>
		<div class="my-auto">
			{friend.firstName}
			{friend.lastName}
		</div>
		<div class="flex-grow" />
	{/if}
</button>
