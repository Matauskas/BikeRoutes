<script lang="ts">
	import { incomingFriendRequests } from '$lib/store';
	import FriendCard from '$lib/components/FriendCard.svelte';
</script>

{#if $incomingFriendRequests.length == 0}
	<div class="text-xl">Nėra pakvietimų</div>
{:else}
	<div class="flex flex-col gap-4">
		{#each $incomingFriendRequests as friendId}
			<FriendCard {friendId} addable />
		{/each}
	</div>
{/if}
