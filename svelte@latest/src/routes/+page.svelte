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
           // Construct URL with query parameters
           let finalUrl = apiRoute;
           if (Object.keys(parameters).length > 0) {
               const queryString = new URLSearchParams(parameters).toString();
               finalUrl = `${apiRoute}?${queryString}`;
           }

		   console.log(finalUrl);

           const headers = {
               'Content-Type': 'application/json'
           };
           
           if (authCode) {
               headers['Authorization'] = authCode;
           }

           const response = await fetch(finalUrl, {
               method: 'GET',
               headers: headers
           });

           const data = await response.json();
           
           // Generate SpecFlow test
           let specFlow = `Scenario: CHANGE\n`;
           specFlow += `    Given an API route "${apiRoute}"\n`;
           
           if (authCode) {
               specFlow += `    And I make a request with the authorization "${authCode}"\n`;
           }

           // Add parameters table if there are any parameters
           if (Object.keys(parameters).length > 0) {
               specFlow += `    Given the parameters\n`;
               specFlow += `      |name|value|\n`;
               Object.entries(parameters).forEach(([key, value]) => {
                   specFlow += `      |${key}|${value}|\n`;
               });
           }
           
           specFlow += `    When I validate the response\n`;
           specFlow += `    Then the response should succeed\n`;

           // Process response properties recursively
           function processProperties(obj, prefix = '') {
               for (const [key, value] of Object.entries(obj)) {
                   const propertyPath = prefix ? `${prefix}.${key}` : key;
                   
                   if (Array.isArray(value)) {
                       specFlow += `    And property ${propertyPath} should be a list with ${value.length} items\n`;
                       // Process array items if they are objects
                       value.forEach((item, index) => {
                           if (typeof item === 'object' && item !== null) {
                               processProperties(item, `${propertyPath}[${index}]`);
                           }
                       });
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
<div class="container bg-gradient-to-b from-gray-50 to-white">
  <h1 class="text-center text-3xl font-bold text-gray-800 mb-12">API to SpecFlow Converter</h1>
  <h2>Note: This only handles query string parameters at the moment</h2>
  
  <div class="card-container">
      <div class="input-section">
          <label class="input-label">API Configuration</label>
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
      
      <div class="input-section mt-8">
          <label class="input-label">Parameters (Optional)</label>
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
          {isLoading ? 'Generating...' : 'Generate SpecFlow'}
      </button>

      <textarea
          readonly
          class="output-textarea"
          value={outputText}
          placeholder="Generated SpecFlow will appear here..."
      />
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
  }

  .card-container {
      background-color: white;
      padding: 2rem;
      border-radius: 1rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
      width: 90%;
      max-width: 1200px;
  }

  .input-section {
      margin-bottom: 2rem;
  }

  .input-label {
      display: block;
      font-size: 1.1rem;
      font-weight: 600;
      color: #4B5563;
      margin-bottom: 1rem;
  }

  .pairs-container {
      width: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
  }

  .input-container {
      display: flex;
      gap: 1.5rem;
      width: 100%;
  }

  .input {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #E5E7EB;
      border-radius: 0.5rem;
      font-size: 0.95rem;
      transition: all 0.2s;
  }

  .input:focus {
      outline: none;
      border-color: #60A5FA;
      box-shadow: 0 0 0 3px rgba(96, 165, 250, 0.2);
  }

  .url-display {
      background-color: #F3F4F6;
      padding: 0.75rem;
      border-radius: 0.5rem;
      color: #4B5563;
      font-family: monospace;
      font-size: 0.9rem;
      width: 100%;
  }

  .generate-button {
      width: 100%;
      padding: 0.75rem;
      background-color: #3B82F6;
      color: white;
      border: none;
      border-radius: 0.5rem;
      font-weight: 500;
      margin: 2rem 0;
      transition: all 0.2s;
  }

  .generate-button:hover:not(:disabled) {
      background-color: #2563EB;
      transform: translateY(-1px);
  }

  .generate-button:disabled {
      background-color: #9CA3AF;
      cursor: not-allowed;
  }

  .output-textarea {
      resize: vertical;
      min-height: 300px;
      width: 100%;
      padding: 1rem;
      border: 1px solid #E5E7EB;
      border-radius: 0.5rem;
      font-family: monospace;
      font-size: 0.9rem;
      background-color: #F9FAFB;
      margin-top: 1rem;
      box-sizing: border-box;
  }

  .output-textarea:focus {
      outline: none;
      border-color: #60A5FA;
      box-shadow: 0 0 0 3px rgba(96, 165, 250, 0.2);
  }
</style>