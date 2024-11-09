<script>
	import { fade, slide } from 'svelte/transition';
	import { createReview } from '$lib/api';
	import Star from './Star.svelte';
	import { createEventDispatcher } from 'svelte';

	export let routeId;

	// Create an event dispatcher
	const dispatch = createEventDispatcher();

	// User rating states
	let rating = null;
	let hoverRating = null;

	// Form interaction states
	let collectFeedback = false;
	let feedbackCompleted = false;

	$: collectFeedback && addWatchListeners();
	$: !collectFeedback && feedbackFormClosed();

	function feedbackFormClosed() {
		feedbackCompleted = false;
		removeWatchListeners();
	}

	function addWatchListeners() {
		document.addEventListener('keydown', userDismissFeedback);
		document.addEventListener('click', userClickedOutsideOfFeedback);
	}
	function removeWatchListeners() {
		document.removeEventListener('keydown', userDismissFeedback);
		document.removeEventListener('click', userClickedOutsideOfFeedback);
	}
	function userClickedOutsideOfFeedback(event) {
		const container = document.getElementById('feedbackContainer');
		if (!container.contains(event.target)) {
			collectFeedback = false;
			dispatch('close');
		}
	}
	function userDismissFeedback(event) {
		switch (event.key) {
			case 'Escape':
				collectFeedback = false;
				dispatch('close');
				break;
			default:
				return;
		}
	}

	const handleHover = (id) => () => {
		hoverRating = id;
	};
	const handleRate = (id) => (event) => {
		if (collectFeedback && rating && rating.toString() === event.srcElement.dataset.starid) {
			collectFeedback = false;
			return;
		}
		rating = id;
		collectFeedback = true;
	};

	let stars = [
		{ id: 1, title: 'One Star' },
		{ id: 2, title: 'Two Stars' },
		{ id: 3, title: 'Three Stars' },
		{ id: 4, title: 'Four Stars' },
		{ id: 5, title: 'Five Stars' }
	];

	async function handleCollectFeedback(e) {
		const textarea = e.srcElement.querySelector('textarea');

		const value = textarea.value;
		const response = await createReview({
			routeId: routeId,
			rating: rating,
			description: value
		});
		// Let them know we received it
		feedbackCompleted = true;

		// Then reset view
		setTimeout(() => {
			collectFeedback = false;
			feedbackCompleted = false;
			dispatch('close');
		}, 1250);
	}

	function handleLinkFeedback() {
		collectFeedback = true;
	}
</script>

<div class="feedback">
	<span
		id="feedbackContainer"
		class="feedbackContainer"
		class:feedbackContainerDisabled={feedbackCompleted}
	>
		<span class="starContainer">
			{#each stars as star (star.id)}
				<Star
					filled={hoverRating ? hoverRating >= star.id : rating >= star.id}
					starId={star.id}
					on:mouseover={handleHover(star.id)}
					on:mouseleave={() => (hoverRating = null)}
					on:click={handleRate(star.id)}
				/>
			{/each}
		</span>
		<br />
		{#if collectFeedback}
			<div class="collectFeedbackContainer" transition:fade>
				{#if feedbackCompleted}
					<p>Ačiū!</p>
				{:else}
					<form
						on:submit|preventDefault={handleCollectFeedback}
						transition:slide={{ duration: 450 }}
					>
						<center
							><p>
								Jūs įvertinote {rating ? rating : 'neįvertinote'} žvaigždut{rating === 1
									? 'e'
									: 'ėmis'}
								<br />
								<br />
								Kodėl taip vertinate?
							</p></center
						>
						<textarea></textarea>
						<center><button> Siųsti įvertinimą </button></center>
						<center
							><button
								on:click={() => {
									collectFeedback = false;
									dispatch('close');
								}}
								type="button"
								class="cancel"
							>
								Ne, ačiū
							</button></center
						>
					</form>
				{/if}
			</div>
		{/if}
	</span>
</div>

<style>
	.feedback {
		position: relative;
	}
	.collectFeedbackContainer {
		width: 100%; /* Adjust width as needed */
		background: #fff;
		border: 1px solid #666;
		padding: 8px;
	}
	.collectFeedbackContainer textarea {
		display: block;
		width: 100%;
		height: 120px;
		resize: none;
		border: 1px solid #ccc; /* Add this line to create a border around the textarea */
		border-radius: 4px; /* Optional: Add border radius for a rounded look */
		padding: 8px; /* Optional: Add padding for space between text and border */
	}
	.cancel {
		background: none;
		text-decoration: underline;
		border: none;
		color: #f44336; /* Red */
		padding: 10px 20px;
		cursor: pointer;
		border-radius: 4px;
	}
	.cancel:hover {
		background-color: #da190b; /* Darker Red */
	}
	.starContainer {
		display: flex; /* Change to flex to make stars appear horizontally */
		transition: background 0.2s ease-out;
		border-radius: 8px;
		padding: 4px 6px 0;
	}
	.feedbackContainer:hover .starContainer {
		background: #efefef;
	}
	.secondaryAction {
		margin: 8px;
		font-size: 12px;
		display: inline-block;
	}
	.feedbackContainerDisabled {
		pointer-events: none;
	}
	button {
		cursor: pointer;
		background-color: #4caf50; /* Green */
		border: none;
		color: white;
		padding: 10px 20px;
		text-align: center;
		text-decoration: none;
		display: inline-block;
		font-size: 16px;
		margin: 4px 2px;
		transition-duration: 0.4s;
		cursor: pointer;
		border-radius: 4px;
	}
	button:hover {
		background-color: #45a049; /* Darker Green */
	}
</style>
