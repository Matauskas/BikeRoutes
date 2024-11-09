<script>
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import * as api from '$lib/api';
	import Button from '$lib/components/Button.svelte';
	import Form from '$lib/components/Form.svelte';
	import FormTextInput from '$lib/components/FormTextInput.svelte';
	import CenterColumnLayout from '$lib/components/CenterColumnLayout.svelte';

	let email = '';
	let token = '';

	let errorMessage = '';
	let emailErrorMessage = '';

	let showPasswordChange = false;
	let confirmPassword = '';
	let newPassword = '';
	let passwordSuccessMessage = '';
	let passwordErrorMessage = '';
	let emailSuccessMessage = '';

	// Patikrina URL dėl tokeno
	function getTokenFromUrl() {
		const urlParams = new URLSearchParams(window.location.search);
		return urlParams.get('token');
	}

	onMount(() => {
		token = getTokenFromUrl() || '';
	});

	async function onSubmit() {
		errorMessage = '';
		emailErrorMessage = '';

		const response = await api.check_email(email);
		if (!response.ok) {
			if (api.containsError(response, 'non_existent', 'Email')) {
				emailErrorMessage = 'Tokio elektroninio pašto nėra';
				return;
			}
			errorMessage = 'Nepavyko prisijungti prie serverio, bandykite dar kartą vėliau';
			return;
		} else {
			emailSuccessMessage = 'Elektroninis laiškas išsiųstas sėkmingai';
		}

		showPasswordChange = true;
	}
	async function onSubmitSlaptazodis() {
		passwordSuccessMessage = '';
		passwordErrorMessage = '';

		if (newPassword !== confirmPassword) {
			passwordErrorMessage = 'Slaptažodžiai nesutampa.';
			return;
		}
		const payload = {
			token: token,
			newPassword: newPassword
		};

		const response = await api.reset_password(newPassword, token);

		if (response.ok) {
			passwordSuccessMessage = 'Slaptažodis pakeistas sėkmingai!';
		} else {
			passwordErrorMessage =
				'Nepavyko pakeisti slaptažodžio, patikrinkite duomenis ir bandykite dar kartą';
		}
	}
</script>

<CenterColumnLayout noNavbar>
	{#if token}
		<Form
			class="mt-10 flex h-full flex-col justify-center gap-10 pb-12"
			on:submit={onSubmitSlaptazodis}
		>
			<div class="text-center text-5xl font-bold">Slaptažodžio keitimas</div>

			<Form class="mb-2 ml-10 flex max-w-sm flex-col gap-2"></Form>
			<FormTextInput
				name="newPassword"
				label="Naujas slaptažodis"
				bind:value={newPassword}
				type="password"
				required
			/>
			<FormTextInput
				name="confirmPassword"
				label="Pakartokite slaptažodį"
				bind:value={confirmPassword}
				type="password"
				required
			/>
			<span>
				<Button variant="green">Patvirtinti</Button>
			</span>
			{#if passwordSuccessMessage}
				<p class="h-4 text-green-600">{passwordSuccessMessage}</p>
			{:else}
				<p class="h-4 text-red-600">{passwordErrorMessage}</p>
			{/if}
		</Form>
	{:else}
		<Form class="mt-10 flex h-full flex-col justify-center gap-10 pb-12" on:submit={onSubmit}>
			<div class="text-center text-5xl font-bold">Slaptažodžio atstatymas</div>

			<div class="flex flex-col gap-2">
				<FormTextInput
					required
					errorMessage={emailErrorMessage}
					bind:value={email}
					name="email"
					label="Elektroninis paštas"
				/>
			</div>

			<span class="mx-auto">
				<Button>Siųsti</Button>
			</span>
			{#if emailSuccessMessage}
				<p class="h-4 text-green-600">{emailSuccessMessage}</p>
			{:else}
				<p class="h-4 text-red-600">{emailErrorMessage}</p>
			{/if}
		</Form>
	{/if}
</CenterColumnLayout>
