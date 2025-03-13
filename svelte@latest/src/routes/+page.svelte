<script>
   let parameters = {};
   let inputPairs = [{ key: "", value: "" }];
   let keyInputs = [];
   let baseUrl = "";
   let endpoint = "";
   let authCode = "";
   let outputText = "";
   let isLoading = false;
   let useBodyParams = false;

   $: apiRoute = baseUrl && endpoint ? 
       (baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl) + 
       (endpoint.startsWith('/') ? endpoint : '/' + endpoint) : '';

   async function handleGenerate() {
       isLoading = true;
       outputText = "";
       
       try {
           // Construct URL and prepare parameters
           let finalUrl = apiRoute;
           const headers = {
               'Content-Type': 'application/json'
           };
           
           if (authCode) {
               headers['Authorization'] = authCode;
           }

           const options = {
               method: useBodyParams ? 'POST' : 'GET',
               headers: headers
           };

           if (Object.keys(parameters).length > 0) {
               if (useBodyParams) {
                   options.body = JSON.stringify(parameters);
               } else {
                   const queryString = new URLSearchParams(parameters).toString();
                   finalUrl = `${apiRoute}?${queryString}`;
               }
           }

           console.log(finalUrl);
           const response = await fetch(finalUrl, options);

           // Generate SpecFlow test
           let specFlow = `Scenario: CHANGE\n`;
           specFlow += `    Given an API route "${apiRoute}"\n`;
           
           if (authCode) {
               specFlow += `    And I make a request with the authorization "${authCode}"\n`;
           }

           // Add parameters based on the mode
           if (Object.keys(parameters).length > 0) {
               if (useBodyParams) {
                   specFlow += `    And request body\n`;
                   specFlow += `      """\n`;
                   specFlow += `      ${JSON.stringify(parameters, null, 6).replace(/^/gm, '      ')}\n`;
                   specFlow += `      """\n`;
               } else {
                   specFlow += `    Given the parameters\n`;
                   specFlow += `      |name|value|\n`;
                   Object.entries(parameters).forEach(([key, value]) => {
                       specFlow += `      |${key}|${value}|\n`;
                   });
               }
           }
           
           specFlow += `    When I validate the response\n`;

           // Handle different status codes
           if (response.status >= 400) {
               specFlow += `    Then Status Code should be the number ${response.status}\n`;
               outputText = specFlow;
               return;
           } else {
               specFlow += `    Then the response should succeed\n`;
           }

           // Only process response body for successful responses
           const data = await response.json();

           // Process response properties recursively with list handling
           function processProperties(obj, prefix = '') {
               for (const [key, value] of Object.entries(obj)) {
                   const propertyPath = prefix ? `${prefix}.${key}` : key;
                   
                   if (Array.isArray(value)) {
                       specFlow += `    And property ${propertyPath} should be a list with ${value.length} items\n`;
                       
                       // Create a map of required properties for list items
                       const requiredProps = new Set();
                       value.forEach(item => {
                           if (typeof item === 'object' && item !== null) {
                               Object.keys(item).forEach(prop => requiredProps.add(prop));
                           }
                       });

                       // For each required property, validate it exists in each item
                       requiredProps.forEach(prop => {
                           specFlow += `    And each item in ${propertyPath} should have property "${prop}"\n`;
                       });

                       // Process the first item's properties as an example
                       if (value.length > 0 && typeof value[0] === 'object' && value[0] !== null) {
                           processProperties(value[0], `${propertyPath}[*]`);
                       }
                   } else if (typeof value === 'object' && value !== null) {
                       processProperties(value, propertyPath);
                   } else {
                       specFlow += `    And property ${propertyPath} should be "${value}"\n`;
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
    <h1>API to SpecFlow Converter</h1>
    <p class="subtitle">Generate SpecFlow scenarios from API responses</p>
    
    <div class="card-container">
        <div class="input-section">
            <div class="section-header">
                <div class="section-icon">⚙️</div>
                <label class="input-label">API Configuration</label>
            </div>
            <div class="input-container mb-6">
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
                <div class="url-display">
                    Full URL: {apiRoute}
                </div>
            {/if}
        </div>
        
        <div class="input-section">
            <div class="section-header">
                <div class="section-icon">🔑</div>
                <label class="input-label">Parameters</label>
            </div>

            <div class="checkbox-container mb-6">
                <label class="checkbox-label">
                    <input
                        type="checkbox"
                        bind:checked={useBodyParams}
                        class="form-checkbox"
                    />
                    <span>Send parameters in request body (uses POST instead of GET)</span>
                </label>
            </div>

            <div class="pairs-container">
                {#each inputPairs as pair, i}
                    <div class="input-container mb-6">
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
        </div>

        <button 
            on:click={handleGenerate}
            class="generate-button"
            disabled={!apiRoute || isLoading}
        >
            <span class="button-text">{isLoading ? 'Generating...' : 'Generate SpecFlow'}</span>
            <span class="button-icon">➜</span>
        </button>

        <div class="output-section">
            <div class="section-header">
                <div class="section-icon">📝</div>
                <label class="input-label">Generated SpecFlow</label>
            </div>
            <textarea
                readonly
                class="output-textarea"
                value={outputText}
                placeholder="Generated SpecFlow will appear here..."
            />
        </div>
    </div>
</div>

<style>
    .container {
        min-height: 100vh;
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 3rem;
        overflow-y: auto;
        background: linear-gradient(135deg, #ffffff 0%, #f0f0f0 100%);
    }

    h1 {
        font-family: var(--font-display);
        font-size: 2.4rem;
        font-weight: 400;
        color: #000000;
        margin-bottom: 1rem;
        text-transform: uppercase;
        letter-spacing: 0.1em;
        text-align: center;
    }

    .subtitle {
        font-family: var(--font-body);
        font-size: 1.1rem;
        color: #404040;
        margin-bottom: 2rem;
        text-align: center;
    }

    .card-container {
        background-color: white;
        padding: 2.5rem;
        border-radius: 1rem;
        box-shadow: 
            -8px -8px 0 rgba(0, 0, 0, 0.2),
            8px 8px 0 rgba(0, 0, 0, 0.1);
        width: 90%;
        max-width: 1200px;
        border: 2px solid #000;
        position: relative;
    }

    .card-container::before {
        content: '';
        position: absolute;
        top: 8px;
        left: 8px;
        right: -8px;
        bottom: -8px;
        background: repeating-linear-gradient(
            45deg,
            rgba(0, 0, 0, 0.05),
            rgba(0, 0, 0, 0.05) 2px,
            transparent 2px,
            transparent 8px
        );
        z-index: -1;
        border-radius: 1rem;
    }

    .input-section {
        margin-bottom: 3rem;
        padding: 0 1rem;
    }

    .input-label {
        display: block;
        font-size: 1.2rem;
        font-family: var(--font-display);
        font-weight: 400;
        color: #000000;
        margin-bottom: 1.5rem;
        text-transform: uppercase;
        letter-spacing: 0.1em;
    }

    .pairs-container {
        width: 100%;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
    }

    .input-container {
        display: flex;
        gap: 1.5rem;
        width: 100%;
        margin-bottom: 1rem;
    }

    .input {
        width: 100%;
        padding: 0.75rem;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        font-size: 0.95rem;
        font-family: var(--font-body);
        transition: all 0.2s;
        background-color: #ffffff;
    }

    .input:focus {
        outline: none;
        border-color: #000000;
        box-shadow: 4px 4px 0 rgba(0, 0, 0, 0.2);
        transform: translate(-2px, -2px);
    }

    .url-display {
        background-color: #F3F4F6;
        padding: 0.75rem;
        border-radius: 0.5rem;
        color: #4B5563;
        font-family: var(--font-mono);
        font-size: 0.9rem;
        width: 100%;
    }

    .generate-button {
        width: 100%;
        padding: 0.75rem;
        background-color: #000000;
        color: white;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        font-family: var(--font-display);
        font-size: 1.2rem;
        font-weight: 400;
        margin: 2rem 0;
        transition: all 0.2s;
        text-transform: uppercase;
        letter-spacing: 0.1em;
        position: relative;
    }

    .generate-button:hover:not(:disabled) {
        background-color: #ffffff;
        color: #000000;
        transform: translate(-4px, -4px);
        box-shadow: 4px 4px 0 #000000;
    }

    .generate-button:disabled {
        background-color: #808080;
        border-color: #808080;
        cursor: not-allowed;
    }

    .output-textarea {
        resize: vertical;
        min-height: 300px;
        width: 100%;
        padding: 1rem;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        font-family: monospace;
        font-size: 0.9rem;
        background-color: #ffffff;
        margin-top: 1rem;
        box-sizing: border-box;
        position: relative;
    }

    .output-textarea:focus {
        outline: none;
        border-color: #000000;
        box-shadow: 4px 4px 0 rgba(0, 0, 0, 0.2);
        transform: translate(-2px, -2px);
    }

    .section-header {
        display: flex;
        align-items: center;
        margin-bottom: 1.5rem;
        padding: 0.5rem 0;
        border-bottom: 2px solid #000000;
    }

    .section-icon {
        font-size: 1.5rem;
        margin-right: 0.75rem;
    }

    .checkbox-container {
        margin: 1.5rem 0;
        padding: 1rem;
        background-color: #f8f8f8;
        border-radius: 0.75rem;
        border: 2px solid #000000;
    }

    .checkbox-label {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        font-family: var(--font-body);
        font-size: 0.95rem;
        color: #000000;
        cursor: pointer;
    }

    .form-checkbox {
        width: 1.25rem;
        height: 1.25rem;
        border: 2px solid #000000;
        border-radius: 0.25rem;
        transition: all 0.2s ease;
    }

    .form-checkbox:checked {
        background-color: #000000;
        border-color: #000000;
    }

    .button-text {
        font-family: var(--font-display);
        font-size: 1.2rem;
        letter-spacing: 0.1em;
    }

    .button-icon {
        font-size: 1.2rem;
        transition: transform 0.3s ease;
    }

    .generate-button:hover .button-icon {
        transform: translateX(5px);
    }
</style>