<script>
	import { goto } from '$app/navigation';
	import * as api from '$lib/api';
	import Button from '$lib/components/Button.svelte';
	import Form from '$lib/components/Form.svelte';
	import FormTextInput from '$lib/components/FormTextInput.svelte';
	import CenterColumnLayout from '$lib/components/CenterColumnLayout.svelte';

	let username = '';
	let password = '';

	let errorMessage = '';
	let usernameErrorMessage = '';
	let passwordErrorMessage = '';

	async function onSubmit() {
		errorMessage = '';
		usernameErrorMessage = '';
		passwordErrorMessage = '';

		// TODO: Add spinner, to show that we are waiting for a response?
		const response = await api.login(username, password);
		if (!response.ok) {
			if (api.containsError(response, 'non_existent', 'Username')) {
				usernameErrorMessage = 'Toks vartotojos neegzistuoja';
				return;
			}
			if (api.containsError(response, 'incorrect', 'Password')) {
				passwordErrorMessage = 'Netinkamas slaptažodis';
				return;
			}

			errorMessage = 'Nepavyko prisijungti prie serverio, bandykite dar kartą vėliau';
			return;
		}

		await goto('/map');
	}
</script>

<CenterColumnLayout noNavbar>
	<Form class="mt-10 flex h-full flex-col justify-center gap-10" on:submit={onSubmit}>
		<div class="text-center text-5xl font-bold">Prisijungti</div>

		<div class="flex flex-col gap-2">
			<FormTextInput
				required
				errorMessage={usernameErrorMessage}
				bind:value={username}
				name="username"
				label="Vartotojo vardas"
			/>
			<FormTextInput
				required
				errorMessage={passwordErrorMessage}
				bind:value={password}
				name="password"
				label="Slaptažodis"
				type="password"
			/>
		</div>

		<div class="text-right">
			<a href="/reset-password" class="text-blue-500 underline">Pamiršau slaptažodį...</a>
		</div>

		<Button>Prisijungti</Button>

		<p class="h-4 text-red-600">{errorMessage}</p>
	</Form>
</CenterColumnLayout>
