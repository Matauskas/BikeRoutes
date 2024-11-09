<script lang="ts">
	let className = '';

	// creates a `class` property, even
	// though it is a reserved word
	export { className as class };
</script>

<hr class="border-slate-400 {className}" />
