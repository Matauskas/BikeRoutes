<script lang="ts">
	import { getUser, updateUser, type User, loggedInUserId } from '$lib/store';
	import CenterColumnLayout from '$lib/components/CenterColumnLayout.svelte';
	import LogoutButton from '$lib/components/LogoutButton.svelte';
	import ProfileInfo from '$lib/components/ProfileInfo.svelte';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import * as api from '$lib/api';

	let loading = true;
	let user: User;
	let email = '';
	let photoUrl = '';
	let showPasswordChange = false;
	let oldPassword = '';
	let newPassword = '';
	let passwordSuccessMessage = '';
	let passwordErrorMessage = '';
	let showDeleteUser = false;
	onMount(async () => {
		const userId: string = $page.params.id;
		if (userId !== String($loggedInUserId)) {
			await goto(`/user/${userId}`);
			return;
		}
		const maybeUser = await getUser(userId);
		console.assert(maybeUser !== undefined);
		user = maybeUser as User;
		email = user.email;
		photoUrl = user.photoUrl;
		console.log(user);
		loading = false;
	});
	async function saveChanges() {
		try {
			await updateUser(user.id, { email, photoUrl });
			console.log('Changes saved successfully');
		} catch (error) {
			console.error('Error saving changes:', error);
		}
	}

	async function updatePassword() {
		passwordSuccessMessage = '';
		passwordErrorMessage = '';

		try {
			const response = await api.updateUserPassword(oldPassword, newPassword);
			if (response.ok) {
				passwordSuccessMessage = 'Slaptažodis atnaujintas sėkmingai';
			} else {
				passwordErrorMessage = 'Nepavyko atnaujinti slaptažodžio';
			}
		} catch (error) {
			//passwordErrorMessage = 'Nepavyko atnaujinti slaptažodžio: ' + error.message;
		}
	}

	async function handleDeleteProfile() {
		try {
			const response = await api.deleteUser();
			if (response.ok) {
				await goto('../');
			} else {
				console.error('Failed to delete profile');
			}
		} catch (error) {
			console.error('Error deleting profile:', error);
		}
	}
</script>

{#if !loading}
	<CenterColumnLayout>
		<LogoutButton slot="left-column" />

		<button
			slot="right-column"
			class="
                box-shadow-green-3 pr-100 relative w-12 rounded-l-lg bg-green-300 p-3
                md:rounded-b-lg md:rounded-tl-none
            "
			on:click={saveChanges}
		>
			<i class="fa-regular fa-floppy-disk fa-xl" />
		</button>

		<ProfileInfo {user} />

		<div class="mt-8">
			<div class="mb-4 text-2xl">Nustatymai</div>
			<div class="mb-3">Elektroninis pašto adresas</div>
			<input
				type="text"
				class="box-shadow-black-3 mb-5 block w-64 rounded-lg bg-slate-50 p-2"
				bind:value={email}
			/>

			<div class="mb-3">Keisti slaptažodį</div>
			{#if showPasswordChange}
				<input
					type="password"
					class="box-shadow-black-3 mb-3 block w-64 rounded-lg bg-slate-50 p-2"
					bind:value={oldPassword}
					placeholder="Dabartinis"
				/>
				<input
					type="password"
					class="box-shadow-black-3 mb-3 block w-64 rounded-lg bg-slate-50 p-2"
					bind:value={newPassword}
					placeholder="Naujas"
				/>
				<div class="flex gap-2">
					<button
						class="box-shadow-black-3 rounded-lg bg-slate-300 px-3 py-1.5"
						on:click={updatePassword}
					>
						Patvirtinti
					</button>
					<button
						class="box-shadow-black-3 rounded-lg bg-slate-300 px-3 py-1.5"
						on:click={() => (showPasswordChange = false)}
					>
						Atšaukti
					</button>
				</div>
				{#if passwordSuccessMessage}
					<p class="h-4 text-green-600">{passwordSuccessMessage}</p>
				{:else}
					<p class="h-4 text-red-600">{passwordErrorMessage}</p>
				{/if}
			{:else}
				<button
					class="box-shadow-black-3 rounded-lg bg-slate-300 px-3 py-1.5"
					on:click={() => (showPasswordChange = true)}
				>
					Keisti slaptažodį
				</button>
			{/if}
		</div>

		<div class="mt-8">
			<div class="mb-4 text-2xl">Pavojaus zona <i class="fa-solid fa-skull" /></div>
			<button
				class="box-shadow-red-3 rounded-lg bg-red-300 px-3 py-1.5"
				on:click={() => (showDeleteUser = !showDeleteUser)}
			>
				Ištrinti paskyrą
			</button>
			{#if showDeleteUser}
				<div class="ml-10">
					<button
						class="box-shadow-red-3 rounded-lg bg-red-300 px-3 py-1.5"
						on:click={handleDeleteProfile}
					>
						Ar tu tuo tikras?
					</button>
				</div>
			{/if}
		</div>
	</CenterColumnLayout>
{/if}
