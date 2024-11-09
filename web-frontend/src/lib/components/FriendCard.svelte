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
	import Button from './Button.svelte';

	export let friendId: Id;
	export let removable = false;
	export let addable = false;

	let friend: User | undefined;
	$: getUser(friendId).then((user) => {
		friend = user;
	});

	async function onRemoveClick() {
		const response = await api.removeFriend(friendId);
		if (!response.ok) return;

		await refreshFriends();
	}

	async function onAddClick() {
		const response = await api.sendFriendRequest(friendId);
		if (!response.ok) return;

		if ($incomingFriendRequests.includes(friendId)) {
			$incomingFriendRequests = $incomingFriendRequests.filter((id) => id !== friendId);
		} else if (!$outgoingFriendRequests.includes(friendId)) {
			$outgoingFriendRequests.push(friendId);
		}

		await refreshFriends();
	}
</script>

{#if friend}
	<a
		href={`/user/${friend.id}`}
		class="box-shadow-black-3 flex flex-row items-center gap-2 rounded-lg bg-slate-50 px-3 py-2 text-lg md:px-4 md:py-3 md:text-xl"
	>
		<div class="flex size-10 items-center justify-center rounded-full bg-slate-300 md:size-14">
			<div class="fa-regular fa-face-grin-squint text-3xl text-slate-500 md:text-4xl" />
		</div>
		<div class="my-auto">
			{friend.firstName}
			{friend.lastName}
		</div>
		<div class="flex-grow" />
		{#if removable}
			<Button size="sm" variant="red" class="mr-auto h-8 w-8" on:click={onRemoveClick}>
				<i class="fa-solid fa-minus" />
			</Button>
		{/if}
		{#if addable}
			<Button size="sm" variant="green" class="mr-auto h-8 w-8" on:click={onAddClick}>
				<i class="fa-solid fa-plus" />
			</Button>
		{/if}
	</a>
{/if}
