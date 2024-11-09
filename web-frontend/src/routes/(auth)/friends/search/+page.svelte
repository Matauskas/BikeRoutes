<script lang="ts">
	import { friends, type Id, type User, outgoingFriendRequests, loggedInUserId } from '$lib/store';
	import FriendCard from '$lib/components/FriendCard.svelte';
	import TextInput from '$lib/components/TextInput.svelte';
	import { onMount } from 'svelte';
	import * as api from '$lib/api';

	let name = '';

	let allUsers: User[] = [];
	onMount(async () => {
		const response = await api.listUsers();
		if (response.ok) {
			allUsers = response.body;
		}
		console.log(allUsers);
	});

	let shownUsers: Id[] = [];
	$: {
		shownUsers = [];
		const lowerName = name.toLocaleLowerCase();
		for (const user of allUsers) {
			if (user.id === $loggedInUserId) {
				continue;
			}

			if (
				user.username.includes(name) ||
				(user.firstName + ' ' + user.lastName).toLocaleLowerCase().includes(lowerName)
			) {
				shownUsers.push(user.id);
			}
		}
	}
</script>

<TextInput name="name" class="text-sm" bind:value={name} placeholder="Vartotojo vardas" />

<div class="mt-4 flex flex-col gap-4">
	{#each shownUsers as userId}
		<FriendCard
			friendId={userId}
			addable={!$friends.includes(userId) && !$outgoingFriendRequests.includes(userId)}
		/>
	{/each}
</div>
