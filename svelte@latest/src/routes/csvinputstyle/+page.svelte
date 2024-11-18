<script>
    import { onMount } from 'svelte';
    import Papa from 'papaparse';

    let inputText = '';
    let outputText = '';
    let isProcessing = false;
    let useBodyParams = false;
    const baseUrl = 'http://vm-hsaws08d'; // Base URL for all requests

    async function validateEndpoint(url, auth = '', params = null, useBody = false) {
        try {
            const headers = {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            };
            
            if (auth) {
                headers['Authorization'] = auth;
            }

            // Construct full URL with base URL
            let fullUrl = url;
            if (!url.startsWith('http')) {
                // Remove any leading slashes from the URL path
                const cleanPath = url.replace(/^\/+/, '');
                fullUrl = `${baseUrl}/${cleanPath}`;
            }
            
            let finalUrl = `https://api.allorigins.win/raw?url=${encodeURIComponent(fullUrl)}`;
            const options = {
                method: useBody ? 'POST' : 'GET',
                headers: headers
            };

            if (params) {
                if (useBody) {
                    options.body = JSON.stringify(params);
                } else {
                    const queryString = new URLSearchParams(params).toString();
                    finalUrl = `${finalUrl}&${queryString}`;
                }
            }

            const response = await fetch(finalUrl, options);

            // We want to return both the response and the parsed data
            const data = await response.json();
            return { response, data };
        } catch (error) {
            console.error('Fetch error:', error);
            throw error;
        }
    }

    async function processCSV(csvText) {
        // Always start with the header row
        if (!csvText.includes('ScenarioName,Authorization,FullURL')) {
            csvText = 'ScenarioName,Authorization,FullURL\n' + csvText;
        }

        const results = Papa.parse(csvText, {
            header: true,
            skipEmptyLines: true,
            transform: (value) => value?.trim() || ''
        });

        if (results.errors.length > 0) {
            return `Error parsing CSV: ${results.errors[0].message}`;
        }

        let output = '';
        let previousAuth = '';

        for (const [index, row] of results.data.entries()) {
            // Skip empty rows
            if (!row.FullURL) {
                continue;
            }

            try {
                const scenarioName = row.ScenarioName || `Scenario ${index + 1}`;
                const auth = row.Authorization === 'prev' ? previousAuth : row.Authorization;
                
                if (index === 0 && auth === 'prev') {
                    output += 'Error: First row cannot use "prev" as Authorization\n\n';
                    continue;
                }

                // Store the current auth for next iteration
                if (auth !== 'prev') {
                    previousAuth = auth;
                }

                // Extract query parameters from row (any additional columns)
                const params = {};
                Object.entries(row).forEach(([key, value]) => {
                    if (!['ScenarioName', 'Authorization', 'FullURL'].includes(key) && value) {
                        params[key] = value;
                    }
                });

                // Make the actual request and get both response and data
                const { response, data } = await validateEndpoint(row.FullURL, auth, params, useBodyParams);
                
                // Start building the SpecFlow scenario
                output += `Scenario: ${scenarioName}\n`;
                output += `    Given an API route "${row.FullURL}"\n`;
                
                if (auth) {
                    output += `    And I make a request with the authorization "${auth}"\n`;
                }

                // Add parameters if any exist
                if (Object.keys(params).length > 0) {
                    output += `    Given the parameters\n`;
                    output += `      | name | value |\n`;
                    Object.entries(params).forEach(([key, value]) => {
                        output += `      | ${key} | ${value} |\n`;
                    });
                }

                output += `    When I validate the response\n`;

                // Handle different response status codes
                if (response.status >= 400) {
                    output += `    Then Status Code should be the number ${response.status}\n\n`;
                    continue;
                } else {
                    output += `    Then the response should succeed\n`;
                }

                // Process response properties recursively
                function processProperties(obj, prefix = '') {
                    for (const [key, value] of Object.entries(obj)) {
                        const propertyPath = prefix ? `${prefix}.${key}` : key;
                        
                        if (Array.isArray(value)) {
                            output += `    And property ${propertyPath} should be a list with ${value.length} items\n`;
                            value.forEach((item, index) => {
                                if (typeof item === 'object' && item !== null) {
                                    processProperties(item, `${propertyPath}[${index}]`);
                                }
                            });
                        } else if (typeof value === 'object' && value !== null) {
                            processProperties(value, propertyPath);
                        } else {
                            output += `    And property ${propertyPath} should be "${value}"\n`;
                        }
                    }
                }

                // Process the response data
                processProperties(data);
                output += '\n';

            } catch (error) {
                output += `Error processing row ${index + 1}: ${error.message}\n\n`;
            }
        }

        return output || 'No valid scenarios to process';
    }

    async function handleProcess() {
        isProcessing = true;
        try {
            outputText = await processCSV(inputText);
        } catch (error) {
            outputText = `Error: ${error.message}`;
        } finally {
            isProcessing = false;
        }
    }

    // Example data to show in the textarea placeholder
    const exampleData = `ScenarioName,Authorization,FullURL
Get User,,vm-hsaw08d/PartnerWebServices.New/api/Employer/GetEmployerProfile
Get Post,,vm-hsaw08d/PartnerWebServices.New/api/Employer/GetEmployerInfo`;
</script>

<div class="container bg-gradient-to-b from-gray-50 to-white">
    <h1 class="text-center text-3xl font-bold text-gray-800 mb-12">Batch CSV to SpecFlow Converter</h1>
    
    <div class="card-container">
        <div class="input-section">
            <label class="input-label">CSV Input</label>
            <div class="mb-4 text-sm text-gray-600 px-4">
                Format (header row optional):<br/>
                ScenarioName,Authorization,FullURL<br/>
                Authorization can be left empty if not needed, or use "prev" to reuse previous row's auth.
            </div>
            <div class="mb-4 px-4 flex items-center">
                <label class="flex items-center cursor-pointer">
                    <input
                        type="checkbox"
                        bind:checked={useBodyParams}
                        class="form-checkbox h-4 w-4 text-blue-600 transition duration-150 ease-in-out"
                    />
                    <span class="ml-2 text-sm text-gray-600">
                        Send parameters in request body (uses POST instead of GET)
                    </span>
                </label>
            </div>
            <textarea
                bind:value={inputText}
                class="input-textarea"
                placeholder={exampleData}
            ></textarea>
        </div>

        <button 
            on:click={handleProcess}
            class="generate-button"
            disabled={!inputText || isProcessing}
        >
            {isProcessing ? 'Processing...' : 'Generate SpecFlow Scenarios'}
        </button>

        <div class="output-section">
            <label class="input-label">Generated SpecFlow</label>
            <textarea
                readonly
                class="output-textarea"
                value={outputText}
                placeholder="Generated SpecFlow scenarios will appear here..."
            ></textarea>
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
    }

    .card-container {
        background-color: white;
        padding: 2rem;
        border-radius: 1rem;
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
        width: 90%;
        max-width: 1200px;
    }

    .input-section, .output-section {
        margin-bottom: 2rem;
        padding: 0 2rem;
    }

    .input-label {
        display: block;
        font-size: 1.1rem;
        font-weight: 600;
        color: #4B5563;
        margin-bottom: 1rem;
    }

    .input-textarea, .output-textarea {
        width: 100%;
        min-height: 200px;
        padding: 1rem;
        border: 1px solid #E5E7EB;
        border-radius: 0.5rem;
        font-family: monospace;
        font-size: 0.9rem;
        resize: vertical;
    }

    .input-textarea {
        background-color: white;
    }

    .output-textarea {
        background-color: #F9FAFB;
    }

    .input-textarea:focus, .output-textarea:focus {
        outline: none;
        border-color: #60A5FA;
        box-shadow: 0 0 0 3px rgba(96, 165, 250, 0.2);
    }

    .generate-button {
        width: calc(100% - 4rem);
        margin: 2rem 2rem;
        padding: 0.75rem;
        background-color: #3B82F6;
        color: white;
        border: none;
        border-radius: 0.5rem;
        font-weight: 500;
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
</style>