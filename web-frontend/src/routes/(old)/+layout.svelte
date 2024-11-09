<script lang="ts">
	import MobileNavbar from './MobileNavbar.svelte';
	import { setup } from '$lib/store';
</script>

{#await setup()}
	<div class="flex h-screen w-screen items-center">
		<div class="mx-auto my-auto text-center text-3xl">Kraunama...</div>
	</div>
{:then}
	<div class="flex h-screen flex-col text-xl">
		<div class="grow overflow-y-scroll">
			<main class="mx-3 max-w-screen-sm sm:mx-auto">
				<slot />
			</main>
		</div>

		<MobileNavbar />
	</div>
{:catch}
	<div class="flex h-screen w-screen items-center">
		<div class="mx-auto my-auto text-center text-3xl">Nu, kažkas atsitiko blogai</div>
	</div>
{/await}
