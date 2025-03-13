<script>
	import { page } from '$app/stores';
	import logo from '$lib/images/svelte-logo.svg';
	import github from '$lib/images/github.svg';
</script>

<header>
	<div class="corner">
		<a href="https://svelte.dev/docs/kit">
			<img src={logo} alt="SvelteKit" />
		</a>
	</div>

	<nav>
		<svg viewBox="0 0 2 3" aria-hidden="true">
			<path d="M0,0 L1,2 C1.5,3 1.5,3 2,3 L2,0 Z" />
		</svg>
		<ul>
			<li aria-current={$page.url.pathname === '/' ? 'page' : undefined}>
				<a href="/">Basic</a>
			</li>
			<li aria-current={$page.url.pathname === '/CSV' ? 'page': undefined}>
				<a href="/csvinputstyle">CSV-Style</a>
			</li>
			<li aria-current={$page.url.pathname === '/DragAndDrop' ? 'page' : undefined}>
				<a href="/responsedraganddrop">Drag-And-Drop</a>
			</li>
		</ul>
		<svg viewBox="0 0 2 3" aria-hidden="true">
			<path d="M0,0 L0,3 C0.5,3 0.5,3 1,2 L2,0 Z" />
		</svg>
	</nav>

	<div class="corner">
		<a href="https://github.com/sveltejs/kit">
			<img src={github} alt="GitHub" />
		</a>
	</div>
</header>

<style>
	header {
		display: flex;
		justify-content: space-between;
		background-color: #ffffff;
		border-bottom: 2px solid #000000;
		padding: 0.5rem 1rem;
		box-shadow: 0 4px 0 rgba(0, 0, 0, 0.1);
	}

	.corner {
		width: 3em;
		height: 3em;
	}

	.corner a {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 100%;
		transition: all 0.2s ease;
	}

	.corner a:hover {
		transform: translate(-2px, -2px);
		filter: drop-shadow(2px 2px 0 rgba(0, 0, 0, 0.3));
	}

	.corner img {
		width: 2em;
		height: 2em;
		object-fit: contain;
	}

	nav {
		display: flex;
		justify-content: center;
		--background: #ffffff;
	}

	svg {
		width: 2em;
		height: 3em;
		display: block;
	}

	path {
		fill: var(--background);
	}

	ul {
		position: relative;
		padding: 0;
		margin: 0;
		height: 3em;
		display: flex;
		justify-content: center;
		align-items: center;
		list-style: none;
		background: var(--background);
		background-size: contain;
		border: 2px solid #000000;
		border-radius: 0.5rem;
	}

	li {
		position: relative;
		height: 100%;
	}

	li[aria-current='page']::before {
		--size: 6px;
		content: '';
		width: 0;
		height: 0;
		position: absolute;
		top: 0;
		left: calc(50% - var(--size));
		border: var(--size) solid transparent;
		border-top: var(--size) solid #000000;
	}

	nav a {
		display: flex;
		height: 100%;
		align-items: center;
		padding: 0 1rem;
		color: #000000;
		font-family: var(--font-display);
		font-weight: 400;
		font-size: 0.9rem;
		text-transform: uppercase;
		letter-spacing: 0.1em;
		text-decoration: none;
		transition: all 0.2s ease;
	}

	nav a:hover {
		color: #000000;
		background-color: rgba(0, 0, 0, 0.05);
		transform: translate(-1px, -1px);
		box-shadow: 2px 2px 0 rgba(0, 0, 0, 0.1);
	}
</style>
