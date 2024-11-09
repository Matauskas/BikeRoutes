<script>
	import Button from '$lib/components/Button.svelte';
	import logotipas from '$lib/images/logo1.png';
	import CenterColumnLayout from '$lib/components/CenterColumnLayout.svelte';
</script>

<CenterColumnLayout noNavbar>
	<img src={logotipas} class="mx-auto mt-10 size-64" alt="Logotipas" />

	<span class="pt-20 text-3xl">
		<span class="font-bold">Bike'čiulis</span>, kuris suteiks galimybę su dviračiu pažinti Kauno
		miestą.
	</span>

	<section class="mx-auto mt-10 flex flex-row gap-8 text-center">
		<Button href="/login" variant="green">Prisijungti</Button>
		<Button href="/registration" variant="green">Registracija</Button>
	</section>
</CenterColumnLayout>
