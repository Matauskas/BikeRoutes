<script>
	import Navbar from '$lib/components/Navbar.svelte';

	export let noNavbar = false;
</script>

<div
	class="
		mx-auto flex h-full min-h-screen max-w-screen-md flex-col border-x-2 border-blue-800 border-opacity-10
		bg-[#E8EBEE]
		px-4 pb-72 md:px-12
		md:pb-8
	"
>
	{#if !noNavbar}
		<div class="flex flex-row gap-3">
			<Navbar showBackButton class="fixed bottom-0 left-0 right-0 md:static" />
			<div class="mb-auto ml-auto flex flex-row gap-4">
				<div class="fixed bottom-28 left-0 md:static">
					<slot name="left-column" />
				</div>
				<div class="fixed bottom-28 right-0 md:static">
					<slot name="right-column" />
				</div>
			</div>
		</div>
	{/if}

	<slot />
</div>
