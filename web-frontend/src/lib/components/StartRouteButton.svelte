<script lang="ts">
	import type { Id } from '$lib/store';

	export let routeId: Id;
</script>

<a
	href={`/trips/${routeId}`}
	class="
        box-shadow-black-3 w-12 rounded-l-lg bg-slate-50 p-3
        md:rounded-b-lg md:rounded-tl-none
    "
>
	<i class="fa-solid fa-flag-checkered fa-lg"></i>
</a>
