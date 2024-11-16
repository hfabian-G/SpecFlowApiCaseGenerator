<script>
   let parameters = {};
   let inputPairs = [{ key: "", value: "" }];
   let keyInputs = [];
   let baseUrl = "";
   let endpoint = "";
   let authCode = "";
   let outputText = "";
   let isLoading = false;

   $: apiRoute = baseUrl && endpoint ? 
       (baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl) + 
       (endpoint.startsWith('/') ? endpoint : '/' + endpoint) : '';

   async function handleGenerate() {
       isLoading = true;
       outputText = "";
       
       try {
           const headers = {
               'Content-Type': 'application/json'
           };
           
           if (authCode) {
               headers['Authorization'] = authCode;
           }

           const response = await fetch(apiRoute, {
               method: 'GET',
               headers: headers
           });

           const data = await response.json();
           
           // Generate SpecFlow test
           let specFlow = `Scenario: CHANGE\n`;
           specFlow += `\tGiven an API route "${apiRoute}"\n`;
           
           if (authCode) {
               specFlow += `\tAnd I make a request with the authorization "${authCode}"\n`;
           }
           
           specFlow += `\tWhen I validate the response\n`;
           specFlow += `\tThen the response should succeed\n`;

           // Process response properties recursively
           function processProperties(obj, prefix = '') {
               for (const [key, value] of Object.entries(obj)) {
                   const propertyPath = prefix ? `${prefix}.${key}` : key;
                   
                   if (Array.isArray(value)) {
                       specFlow += `\tAnd property ${propertyPath} should be a list with ${value.length} items\n`;
                       // Process array items if they are objects
                       value.forEach((item, index) => {
                           if (typeof item === 'object' && item !== null) {
                               processProperties(item, `${propertyPath}[${index}]`);
                           }
                       });
                   } else if (typeof value === 'object' && value !== null) {
                       processProperties(value, propertyPath);
                   } else {
                       specFlow += `\tAnd property ${propertyPath} should be "${value}"\n`;
                   }
               }
           }

           processProperties(data);
           outputText = specFlow;

       } catch (error) {
           outputText = `Error: ${error.message}`;
       } finally {
           isLoading = false;
       }
   }

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
   
   <div class="input-container mb-4">
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

   <div class="input-container mb-8">
       <input 
           type="text" 
           bind:value={authCode} 
           placeholder="Basic Auth (e.g. Basic dXNlcjpwYXNz...)"
           class="input"
       />
   </div>

   {#if apiRoute}
       <div class="text-sm text-gray-600 mb-8 w-full max-w-800px">
           Full URL: {apiRoute}
       </div>
   {/if}
   
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

   <button 
       on:click={handleGenerate}
       class="mb-4 px-6 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 disabled:bg-gray-400"
       disabled={!apiRoute || isLoading}
   >
       {isLoading ? 'Generating...' : 'Generate SpecFlow'}
   </button>

   <textarea
    readonly
    style="width: 70vw"
    class="h-64 p-4 border rounded font-mono text-sm"
    value={outputText}
    placeholder="Generated SpecFlow will appear here..."
/>
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

   textarea {
       resize: vertical;
       min-height: 200px;
       background-color: #f8f9fa;
   }
</style>