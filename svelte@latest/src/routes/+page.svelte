<script>
	let parameters = {};
	let inputPairs = [{ key: "", value: "" }];
    let keyInputs = [];
    let apiRoute = "";
    let baseUrl = ""; // e.g. https://api.example.com
    let endpoint = ""; // e.g. /api/v1/users

    // Combine baseUrl and endpoint to form full apiRoute
    $: apiRoute = baseUrl && endpoint ? 
        (baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl) + 
        (endpoint.startsWith('/') ? endpoint : '/' + endpoint) : '';

	function handleValueBlur(index) {
		if (inputPairs[index].key && inputPairs[index].value) {
			parameters[inputPairs[index].key] = inputPairs[index].value;
			parameters = {...parameters};
			addNewPair();
		} else if (inputPairs[index].key === "" && inputPairs[index].value === "" && inputPairs.length > 1) {
			inputPairs = inputPairs.filter((_, i) => i !== index);
			const oldKey = Object.keys(parameters)[index];
			if (oldKey) {
				delete parameters[oldKey];
				parameters = {...parameters};
			}
		}
	}

	function handleKeyInput(index) {
		if (inputPairs[index].key === "") {
			const oldKey = Object.keys(parameters)[index];
			if (oldKey) {
				delete parameters[oldKey];
				parameters = {...parameters};
			}
			if (inputPairs.length > 1) {
				inputPairs = inputPairs.filter((_, i) => i !== index);
			}
		}
	}

	function addNewPair() {
		inputPairs = [...inputPairs, { key: "", value: "" }];
        setTimeout(() => {
            keyInputs[keyInputs.length - 1].focus({preventScroll: true});
        }, 0);
	}
</script>

<div class="container">
	<h1 class="text-center mb-8">API to SpecFlow Converter</h1>
	
    <div class="input-container mb-8">
        <input 
            type="text" 
            bind:value={baseUrl} 
            placeholder="Base URL (e.g. https://api.example.com)"
            class="input"
        />
        <input 
            type="text" 
            bind:value={endpoint} 
            placeholder="Endpoint (e.g. /api/v1/users)"
            class="input"
        />
    </div>
	<br/>

    {#if apiRoute}
        <div class="text-sm text-gray-600 mb-8 w-full max-w-800px">
            Full URL: {apiRoute}
        </div>
    {/if}
	<br/>
	
	<div class="pairs-container">
		{#each inputPairs as pair, i}
			<div class="input-container mb-4">
				<input 
					type="text" 
					bind:value={pair.key} 
					placeholder="Key"
					class="input"
					bind:this={keyInputs[i]}
					on:input={() => handleKeyInput(i)}
				/>
				<input 
					type="text" 
					bind:value={pair.value} 
					placeholder="Value"
					class="input"
					on:blur={() => handleValueBlur(i)}
				/>
			</div>
		{/each}
	</div>

	<pre class="mt-4">{JSON.stringify(parameters, null, 2)}</pre>
</div>

<style>
	.container {
		height: 100vh;
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 2rem;
		overflow-y: auto;
	}

	.pairs-container {
		width: 100%;
		display: flex;
		flex-direction: column;
		align-items: center;
	}

	.input-container {
		display: flex;
		gap: 1rem;
		width: 100%;
		max-width: 800px;
	}

	.input {
		width: 100%;
		padding: 0.5rem;
		border: 1px solid #ccc;
		border-radius: 4px;
	}
</style>