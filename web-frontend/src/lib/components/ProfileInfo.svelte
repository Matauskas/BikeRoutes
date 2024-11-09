<script lang="ts">
	import type { User } from '$lib/store';

	export let user: User;

	function round1(x: number): number {
		return Math.round(x * 10) / 10;
	}
</script>

<div class="flex flex-row gap-8 pt-8">
	<div class="flex size-24 items-center justify-center rounded-full bg-slate-300 sm:size-32">
		<div class="fa-regular fa-face-grin-squint text-6xl text-slate-500 sm:text-8xl" />
	</div>
	<div class="flex flex-col">
		<div class="text-3xl font-semibold sm:text-4xl">
			{user.firstName}
			{user.lastName}
		</div>
		<!-- TODO: -->
		<!-- <div class="font-normal">Užsiregistravo prieš 2 mėnesius</div> -->
		<div class="h-4 flex-grow" />
		<div class="flex flex-row gap-3 text-center text-xl sm:text-2xl">
			<div>
				<div class="font-semibold">{round1(user.totalDistance)}km</div>
				<div>Nuvažiuota</div>
			</div>
			<div>
				<div class="font-semibold">{round1(user.totalTime)}h</div>
				<div>Laikas</div>
			</div>
			<div>
				<div class="font-semibold">{user.totalCount}</div>
				<div>Kiekis</div>
			</div>
		</div>
	</div>
</div>
