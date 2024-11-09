<script lang="ts">
	import { logout } from '$lib/store';
	export let show = true;
</script>

<button
	class="
        box-shadow-red-3 rounded-r-lg bg-red-300 p-3
        md:rounded-b-lg md:rounded-tr-none
    "
	class:hidden={!show}
	on:click={logout}
>
	<i class="fa-solid fa-right-from-bracket fa-lg" />
</button>
