<script>
	export let isOpen = false;

	function close() {
		isOpen = false;
	}
</script>

{#if isOpen}
	<div class="overlay" on:click={close}></div>
	<div class="popup">
		<slot />
		<center><button class="center-button" on:click={close}>Uždaryti</button></center>
	</div>
{/if}

<style>
	.popup {
		position: fixed;
		top: 50%;
		left: 50%;
		transform: translate(-50%, -50%);
		background: white;
		padding: 20px;
		box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
		z-index: 1000;
	}
	.overlay {
		position: fixed;
		top: 0;
		left: 0;
		width: 100%;
		height: 100%;
		background: rgba(0, 0, 0, 0.5);
		z-index: 999;
	}
</style>
