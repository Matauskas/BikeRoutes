<script lang="ts">
	import { friends } from '$lib/store';
	import FriendCard from '$lib/components/FriendCard.svelte';
</script>

{#if $friends.length === 0}
	<div class="text-xl">
		Neturi draugų <i class="fa-regular fa-face-sad-tear align-middle" />
	</div>
{:else}
	<div class="flex flex-col gap-4">
		{#each $friends as friendId}
			<FriendCard {friendId} removable />
		{/each}
	</div>
{/if}
