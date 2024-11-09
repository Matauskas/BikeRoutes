import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ command, mode }) => {
	const env = loadEnv(mode, process.cwd())
	const apiUrl = env.VITE_API_URL;

	return {
		plugins: [sveltekit()],
		server: {
			proxy: { '/api': apiUrl }
		}
	}
});
