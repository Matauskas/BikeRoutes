<script lang="ts">
	import * as api from '$lib/api';
	import Button from '$lib/components/Button.svelte';
	import Form from '$lib/components/Form.svelte';
	import ResultMessage from '$lib/components/ResultMessage.svelte';
	import FormTextInput from '$lib/components/FormTextInput.svelte';
	import CenterColumnLayout from '$lib/components/CenterColumnLayout.svelte';

	let first_name = '';
	let last_name = '';
	let username = '';
	let email = '';
	let password = '';
	let confirmPassword = '';
	let errorMessage = '';
	let successMessage = '';

	let usernameErrorMessage = '';
	let emailErrorMessage = '';

	function validateUsername(value: string) {
		if (/^[0-9_]+$/.test(value)) {
			return {
				isValid: false,
				message: 'Vartotojo vardas negali būti sudarytas tiktais iš skaitmenų!'
			};
		}

		return {
			isValid: /^[a-zA-Z0-9_]+$/.test(value),
			message: 'Vartotojo vardas gali būti sudarytas tik iš raidžių, skaitmenų ir pabraukimų!'
		};
	}

	function validateFirstName(value: string) {
		return {
			isValid: /^[a-zA-Z]+$/.test(value),
			message: 'Vardas privalo būti tik iš raidžių.'
		};
	}

	function validateLastName(value: string) {
		return {
			isValid: /^[a-zA-Z]+$/.test(value),
			message: 'Pavardė privalo būti tik iš raidžių.'
		};
	}

	function validatePassword(value: string) {
		return {
			isValid: value.length >= 8,
			message: 'Slaptažodis turi būti bent 8 simbolių ilgio!'
		};
	}

	function validateConfirmPassword() {
		return {
			isValid: password === confirmPassword,
			message: 'Slaptažodžiai nesutampa!'
		};
	}

	function containsError(errors: any[], field: string, reason: string) {
		return errors.some((err) => err.field === field && err.reason === reason);
	}

	async function onSubmit() {
		successMessage = '';
		usernameErrorMessage = '';
		emailErrorMessage = '';

		const response = await api.register({
			username,
			password,
			email,
			firstName: first_name,
			lastName: last_name
		});
		if (!response.ok) {
			if (api.containsError(response, 'occupied', 'Username')) {
				usernameErrorMessage = 'Toks vartotojo vardas jau yra naudojamas';
				return;
			}
			if (api.containsError(response, 'occupied', 'Email')) {
				emailErrorMessage = 'Toks elektroninis paštas jau yra naudojamas';
				return;
			}

			errorMessage = 'Nepavyko užsiregistruoti, bandykite dar kartą vėliau';
			return;
		}

		successMessage = 'Sėkmingai užsiregistruota';
	}
</script>

<CenterColumnLayout noNavbar>
	<Form class="mt-10 flex h-full flex-col justify-center gap-10 pb-12" on:submit={onSubmit}>
		<div class="text-center text-5xl font-bold">Registracija</div>

		<div class="flex flex-col gap-2">
			<FormTextInput
				required
				errorMessage={usernameErrorMessage}
				validate={validateUsername}
				bind:value={username}
				name="username"
				label="Vartotojo vardas"
			/>
			<FormTextInput
				required
				validate={validateFirstName}
				bind:value={first_name}
				name="first_name"
				label="Vardas"
			/>
			<FormTextInput
				required
				validate={validateLastName}
				bind:value={last_name}
				name="last_name"
				label="Pavardė"
			/>
			<FormTextInput
				required
				errorMessage={emailErrorMessage}
				bind:value={email}
				name="email"
				label="Elektroninis paštas"
				type="email"
			/>
			<FormTextInput
				required
				bind:value={password}
				validate={validatePassword}
				name="password"
				label="Slaptažodis"
				type="password"
			/>
			<FormTextInput
				required
				bind:value={confirmPassword}
				validate={validateConfirmPassword}
				name="confirm_password"
				label="Patvirtinti slaptažodį"
				type="password"
			/>
		</div>

		<span class="mx-auto">
			<Button>Registruotis</Button>
		</span>

		<ResultMessage {successMessage} {errorMessage} />
	</Form>
</CenterColumnLayout>
