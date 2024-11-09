<script lang="ts">
	import * as store from '$lib/store';
	import * as api from '$lib/api';
	import Button from '$lib/components/Button.svelte';
	import TextInput from '$lib/TextInput.svelte';
	import FormTextInput from '$lib/FormTextInput.svelte';
	import Form from '$lib/Form.svelte';
	import type { User } from '$lib/store';
	import { goto } from '$app/navigation';

	let email = '';
	let photoUrl = '';

	let showPasswordChange = false;
	let oldPassword = '';
	let newPassword = '';
	let passwordSuccessMessage = '';
	let passwordErrorMessage = '';
	let logoutErrorMessage = '';

	let showDeleteUser = false;

	function validateNewPassword(value: string) {
		return {
			isValid: value.length >= 8,
			message: 'Slaptažodis turi būti bent 8 simbolių ilgio!'
		};
	}

	async function saveUser(user: User | undefined) {
		const userId = user?.id as number;
		await store.updateUser(userId, { email, photoUrl });
	}

	async function updatePassword() {
		passwordSuccessMessage = '';
		passwordErrorMessage = '';

		const response = await api.updateUserPassword(oldPassword, newPassword);
		if (response.ok) {
			passwordSuccessMessage = 'Slaptažodis atnaujintas sėkmingai';
		} else {
			// TODO: Better error mesages
			passwordErrorMessage = 'Nepavyko atnaujinti slaptažodžio';
		}
	}

	async function deleteUser() {
		const response = await api.deleteUser();
		if (!response.ok) {
			// TODO: Show error message
			return;
		}
		store.logout();

		await goto('/');
	}

	async function onSubmit() {
		try {
			logoutErrorMessage = '';
			await api.logout();
			await goto('/');
		} catch (error) {
			logoutErrorMessage = error.message;
		}
	}

	async function handleImageUpload() {
		const input = document.getElementById('imageInput');
		const file = input.files[0];
		if (file) {
			const formData = new FormData();
			formData.append('file', file);

			try {
				const response = await fetch('/api/Images/upload', {
					method: 'POST',
					body: formData
				});

				if (response.ok) {
					console.log('Ikėlimas sėkmingas');

					window.location.reload();
				} else {
					console.error('Nepavyko:', response.statusText);
				}
			} catch (error) {
				console.error('Nepavyko:', error);
			}
		}
	}

	function openFileInput() {
		document.getElementById('imageInput').click();
	}
</script>

{#await store.getLoggedInUser() then user}
	<!-- TODO: User in here should always be not undefined, but too lazy to add checks -->

	<div class="mt-10 flex h-full flex-col gap-8">
		<div class="relative flex gap-3">
			<!-- svelte-ignore a11y-click-events-have-key-events -->
			<div class="flex items-center">
				<!-- svelte-ignore a11y-no-static-element-interactions -->
				<div
					id="imagePreviewContainer"
					class="flex items-center justify-center overflow-hidden rounded-full bg-slate-400"
					style="width: 128px; height: 128px; margin-right: 10px"
					on:click={openFileInput}
				>
					{#if user?.photoUrl}
						<img
							src="/api/Images/receive?userId={user.id}"
							alt="Tuščia"
							style="width: 100%; height: 100%;"
						/>
					{:else}
						<i id="imageIcon" class="far fa-image cursor-pointer text-4xl"></i>
					{/if}
				</div>
				<div class="text-3xl">{user?.firstName} {user?.lastName}</div>
			</div>
			<input
				type="file"
				id="imageInput"
				class="hidden"
				accept="image/*"
				on:change={handleImageUpload}
			/>
		</div>
		<script>
		</script>

		<div class="flex flex-col gap-3">
			<div class="mb-2 border-b border-slate-400 text-2xl font-semibold">Nustatymai</div>
			<TextInput
				name="email"
				label="Paštas"
				initialValue={user?.email}
				bind:value={email}
				class="max-w-sm"
			/>
			<span><Button variant="primary" on:click={() => saveUser(user)}>Saugoti</Button></span>
		</div>

		<span>
			<Button variant="secondary" href="/user/friends-list">Draugų sąrašas</Button>
		</span>

		<div class="mb-4 flex flex-col gap-3">
			<div class="mb-2 border-b border-slate-400 text-2xl font-semibold">Pavojaus zona</div>
			<span>
				<Button variant="secondary" on:click={() => (showPasswordChange = !showPasswordChange)}>
					Keisti slaptažodį
				</Button>
			</span>
			{#if showPasswordChange}
				<Form class="mb-2 ml-10 flex max-w-sm flex-col gap-2" on:submit={updatePassword}>
					<!-- TODO: make this a modal -->
					<FormTextInput
						name="oldPassword"
						label="Senas slaptažodis"
						bind:value={oldPassword}
						type="password"
						required
					/>
					<FormTextInput
						name="newPassword"
						label="Naujas slaptažodis"
						bind:value={newPassword}
						validate={validateNewPassword}
						type="password"
						required
					/>
					<span>
						<Button variant="secondary" on:click={() => (showPasswordChange = false)}>
							Atšaukti
						</Button>
						<Button variant="primary">Patvirtinti</Button>
					</span>
					{#if passwordSuccessMessage}
						<p class="h-4 text-green-600">{passwordSuccessMessage}</p>
					{:else}
						<p class="h-4 text-red-600">{passwordErrorMessage}</p>
					{/if}
				</Form>
			{/if}
			<span>
				<Button variant="secondary" on:click={() => (showDeleteUser = !showDeleteUser)}>
					Ištrinti paskyrą
				</Button>
			</span>
			<span>
				<Button variant="red" on:click={onSubmit}>Atsijungti</Button>
			</span>
			{#if showDeleteUser}
				<div class="ml-10">
					<Button variant="secondary" on:click={deleteUser}>Ar tu tuo tikras?</Button>
				</div>
			{/if}
		</div>
	</div>
{/await}
