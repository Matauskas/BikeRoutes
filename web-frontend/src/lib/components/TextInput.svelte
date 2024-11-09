<svelte:options accessors />

<script lang="ts">
	import type { FormEventHandler } from 'svelte/elements';

	interface ValidateResult {
		isValid: boolean;
		message?: string;
	}
	type ValidateCallback = (value: string) => ValidateResult;

	export let name: string;
	export let initialValue: string | undefined = undefined;
	export let value: string = '';
	export let label: string | undefined = undefined;
	export let type: string = 'text';
	export let required: boolean = false;
	export let validate: ValidateCallback | undefined = undefined;
	export let errorMessage: string = '';
	export let placeholder: string = '';

	let builtinErrorMessage = '';
	let shownErrorMessage = '';
	$: {
		shownErrorMessage = builtinErrorMessage || errorMessage;
	}

	if (initialValue) {
		value = initialValue;
	}

	export function refreshErrorMessage() {
		value = value || '';
		builtinErrorMessage = '';
		if (required && value === '') {
			builtinErrorMessage = 'Privaloma užpildyti';
			return false;
		}
		if (validate) {
			const result = validate(value);
			if (!result.isValid) {
				builtinErrorMessage = result.message || '';
				return false;
			}
		}

		return true;
	}

	const handleInput: FormEventHandler<HTMLInputElement> = (e) => {
		value = e.currentTarget.value || '';

		refreshErrorMessage();
	};
</script>

<div class="flex flex-col {$$props.class}">
	{#if label}
		<label for={name}>
			{label}
			{#if required}
				<span class="text-red-500">*</span>
			{/if}
		</label>
	{/if}
	<input
		class:bg-red-300={shownErrorMessage}
		class:box-shadow-red-3={shownErrorMessage}
		value={value || ''}
		on:input={handleInput}
		{placeholder}
		{name}
		{type}
		class="box-shadow-black-3 mt-1 rounded-r-lg p-1"
	/>
	{#if shownErrorMessage}
		<div class="mt-1 text-red-500">
			{shownErrorMessage}
		</div>
	{/if}
</div>
