<!-- Reviews.svelte -->
<script>
	export let reviews;

	function getStars(rating) {
		let stars = '';
		for (let i = 0; i < rating; i++) {
			stars += '★'; // Unicode character for star
		}
		return stars;
	}
</script>

<div class="container">
	<div class="square">
		<h2>Komentarai</h2>
		<div class="scrollable">
			{#if reviews && reviews.length > 0}
				<ul>
					{#each reviews as review}
						<li>
							<p>
								<strong>Reitingas:</strong> <span class="rating">{getStars(review.rating)}</span>
							</p>
							<p class="description"><strong>Komentaras:</strong> {review.description}</p>
						</li>
					{/each}
				</ul>
			{:else}
				<p class="no-reviews">No reviews found.</p>
			{/if}
		</div>
	</div>
</div>

<style>
	/* Container styles */
	.container {
		display: flex;
		justify-content: center;
		align-items: center;
		position: fixed;
		top: 0;
		left: 0;
		width: 100%;
		height: 100%;
		background-color: rgba(0, 0, 0, 0.5); /* Semi-transparent background */
		z-index: 999; /* Ensure the modal appears on top of other content */
	}

	/* Square styles */
	.square {
		width: 300px;
		max-height: 400px; /* Maximum height before scrolling */
		background-color: #f8f8f8;
		border-radius: 8px;
		box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
		padding: 20px;
	}

	/* Scrollable content */
	.scrollable {
		overflow-y: auto; /* Enable vertical scrolling */
		max-height: 320px; /* Height of the scrollable area */
	}

	/* Title styles */
	h2 {
		font-size: 24px;
		color: #333;
		margin-bottom: 15px;
		text-align: center;
	}

	/* List styles */
	ul {
		list-style-type: none;
		padding: 0;
		margin: 0;
	}

	li {
		border-bottom: 1px solid #ddd;
		padding: 15px 0;
	}

	/* Rating styles */
	.rating {
		color: #ffc107; /* Yellow color for stars */
		font-size: 18px;
	}

	/* Description styles */
	.description {
		color: #666;
		font-size: 16px;
		margin-top: 5px;
	}

	/* No reviews message styles */
	.no-reviews {
		font-style: italic;
		color: #999;
	}
</style>
