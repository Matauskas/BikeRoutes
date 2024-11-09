<script lang="ts">
	export let showModal = false; // boolean

	let dialog: HTMLDialogElement;

	$: if (dialog && showModal) dialog.showModal();
	$: if (dialog && !showModal) dialog.close();
</script>

<!-- svelte-ignore a11y-click-events-have-key-events a11y-no-noninteractive-element-interactions -->
<dialog
	bind:this={dialog}
	on:close={() => (showModal = false)}
	on:click|self={() => dialog.close()}
	class="dialog-backdrop-black-40 max-w-lg rounded-lg bg-slate-50"
>
	<div class="p-4">
		<div class="flex justify-between">
			<slot name="title" />
			<button class="ml-2" on:click={() => dialog.close()}>
				<i class="fa-solid fa-xmark" />
			</button>
		</div>

		<hr />
		<slot />
	</div>
</dialog>

<style>
	.dialog-backdrop-black-40::backdrop {
		background: rgba(0, 0, 0, 0.4);
	}
</style>
