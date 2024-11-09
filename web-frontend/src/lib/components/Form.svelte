<script lang="ts">
	import { createEventDispatcher, setContext } from 'svelte';
	import { writable } from 'svelte/store';
	import type { Writable } from 'svelte/store';
	import TextInput from './TextInput.svelte';

	const dispatch = createEventDispatcher();

	let form: Writable<TextInput[]> = writable([]);
	setContext('form', form);

	function onSubmit() {
		let errorOccured = false;
		for (const formInput of $form) {
			const isFormInputCorrect = formInput.refreshErrorMessage();
			errorOccured ||= !isFormInputCorrect;
		}

		if (!errorOccured) {
			dispatch('submit');
		}
	}
</script>

<form {...$$restProps} on:submit|preventDefault={onSubmit}>
	<slot />
</form>
