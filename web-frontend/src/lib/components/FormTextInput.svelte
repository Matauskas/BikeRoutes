<script lang="ts">
	// @ts-nocheck

	import TextInput from '$lib/components/TextInput.svelte';
	import { getContext, onMount } from 'svelte';

	export let value = '';
	let element;
	const form = getContext('form');
	onMount(() => {
		$form.push(element);
	});
</script>

<TextInput bind:this={element} bind:value {...$$props} />
