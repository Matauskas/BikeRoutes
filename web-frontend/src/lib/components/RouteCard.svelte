<script lang="ts">
	import type { Route } from '$lib/store';

	export let route: Route;

	function round1(x: number): number {
		return Math.round(x * 10) / 10;
	}
</script>

<a
	class="box-shadow-black-3 flex h-20 w-full flex-row items-center justify-between rounded-lg bg-slate-50 text-sm sm:text-base"
	href={`/routes/${route.id}`}
>
	<div class="bg-stripped hidden h-full w-40 rounded-l-lg sm:block" />
	<div class="ml-3 flex-grow text-base sm:ml-6 sm:text-lg md:text-xl">{route.title}</div>
	<div class="flex flex-row gap-3 pr-3 sm:pr-6">
		<!-- TODO: -->
		<!-- <div>
			<div class="text-center font-semibold">9/10</div>
			<div>Sudėtingumas</div>
		</div> -->
		<div>
			<div class="text-center font-semibold">{round1(route.distance)}km</div>
			<div>Atstumas</div>
		</div>
		<div>
			<div class="text-center font-semibold">{round1(route.time)}h</div>
			<div>Laikas</div>
		</div>
	</div>
</a>
