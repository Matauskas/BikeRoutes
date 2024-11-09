<script lang="ts">
	export let variant: 'basic' | 'green' | 'red' = 'basic';
	export let size: 'sm' | 'base' | 'lg' = 'base';

	const sizes = {
		lg: 'px-6 py-3 text-2xl',
		base: 'px-4 py-2',
		sm: 'px-2 py-1 text-sm'
	};

	const styles = {
		basic: 'bg-slate-50 box-shadow-black-3',
		green: 'bg-green-300 box-shadow-green-3',
		red: 'bg-red-300 box-shadow-red-3'
	};

	let sizeStyle = '';
	$: sizeStyle = sizes[size] || '';

	let variantStyle = '';
	$: variantStyle = styles[variant] || '';

	let customClass = '';
	let otherProps = $$restProps;
	$: {
		({ class: customClass, ...otherProps } = $$restProps);
		if (customClass === undefined) customClass = '';
	}
</script>

{#if otherProps.href}
	<!-- svelte-ignore a11y-missing-attribute -->
	<!-- svelte-ignore a11y-no-static-element-interactions -->
	<a
		class="rounded-lg text-center font-semibold {sizeStyle} {variantStyle} {customClass}"
		on:click
		{...otherProps}
	>
		<slot />
	</a>
{:else}
	<button class="rounded-lg {sizeStyle} {variantStyle} {customClass}" on:click {...otherProps}>
		<slot />
	</button>
{/if}
