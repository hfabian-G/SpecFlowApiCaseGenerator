<script>
import { onMount } from 'svelte';

// State variables
let inputText = '';
let outputText = '';
let scenarioName = 'API Response';
let apiRoute = '';
let authCode = '';
let isProcessing = false;
let parameters = {};
let inputPairs = [{ key: "", value: "" }];
let keyInputs = [];
let filterDateAndNull = false;

function hasEmptyPair() {
    return inputPairs.some(pair => pair.key === "" && pair.value === "");
}

function handleValueBlur(index) {
    if (inputPairs[index].key && inputPairs[index].value) {
        parameters[inputPairs[index].key] = inputPairs[index].value;
        parameters = {...parameters};
        
        // Only add a new pair if there isn't already an empty pair
        if (!hasEmptyPair()) {
            addNewPair();
        }
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
        // Only remove the pair if it's not the last empty pair
        if (inputPairs.length > 1 && 
            inputPairs.filter(pair => pair.key === "" && pair.value === "").length > 1) {
            inputPairs = inputPairs.filter((_, i) => i !== index);
        }
    }
}

function addNewPair() {
    // Only add a new pair if there isn't already an empty pair
    if (!hasEmptyPair()) {
        inputPairs = [...inputPairs, { key: "", value: "" }];
        setTimeout(() => {
            keyInputs[keyInputs.length - 1]?.focus({preventScroll: true});
        }, 0);
    }
}

function shouldIncludeResponseProperty(key, value) {
    if (!filterDateAndNull) return true;
    
    if (key.includes('Date') || key.includes('date') || value === null || value === "null") {
        return false;
    }
    return true;
}

function formatValue(value) {
    // Special handling for boolean values
    if (value == 'true') {
        return 'True';
    } else if (value == 'false'){
        return 'False';
    }
    return `${value}`;
}

function processResponse(jsonText, name = 'API Response') {
    try {
        const data = JSON.parse(jsonText);
        let output = '';

        output += `Scenario: ${name}\n`;
        
        if (apiRoute) {
            output += `    Given I send a POST request to ${apiRoute}\n`;
        }
        
        if (authCode) {
            output += `    And header Authorization equals to "${authCode}"\n`;
        }

        if (Object.keys(parameters).length > 0) {
            output += `    And properties\n`;
            output += `      | name | value |\n`;
            Object.entries(parameters).forEach(([key, value]) => {
                output += `      | ${key} | ${value} |\n`;
            });
        }

        output += `    When I validate the response\n`;
        output += `    Then the request should succeed\n`;

        function processProperties(obj, prefix = '') {
            for (const [key, value] of Object.entries(obj)) {
                const propertyPath = prefix ? `${prefix}.${key}` : key;
                
                if (!shouldIncludeResponseProperty(propertyPath, value)) {
                    continue;
                }

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
                    output += `    And property ${propertyPath} should be "${formatValue(value)}"\n`;
                }
            }
        }

        processProperties(data);
        return output;
    } catch (error) {
        return `Error processing JSON: ${error.message}`;
    }
}

function handleProcess() {
    isProcessing = true;
    try {
        outputText = processResponse(inputText, scenarioName);
    } catch (error) {
        outputText = `Error: ${error.message}`;
    } finally {
        isProcessing = false;
    }
}

const exampleData = `{
    "id": 1,
    "name": "Test User",
    "email": "test@example.com",
    "createdDate": "2024-01-01",
    "details": {
        "age": 30,
        "active": true,
        "lastLoginDate": null
    },
    "roles": ["user", "admin"]
}`;
</script>

<div class="container bg-gradient-to-br from-blue-50 via-white to-indigo-50">
    <div class="title-section">
        <h1 class="text-center text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-indigo-600 mb-4">
            JSON to SpecFlow Converter
        </h1>
        <p class="text-center text-gray-600 mb-12">Transform your JSON responses into SpecFlow scenarios</p>
        <p class="text-center text-gray-600 mb-12">This page deals with scenarios where CORS does not allow a direct call & response</p>
    </div>
    
    <div class="card-container">
        <div class="input-section">
            <div class="section-header">
                <div class="section-icon">⚙️</div>
                <label class="input-label">API Configuration</label>
            </div>
            <div class="input-container mb-6 hover-effect">
                <input 
                    type="text" 
                    bind:value={apiRoute} 
                    placeholder="API Route (e.g. https://api.example.com/users)"
                    class="input"
                />
            </div>
            <br/>
            <div class="input-container mb-8 hover-effect">
                <input 
                    type="text" 
                    bind:value={authCode} 
                    placeholder="Basic Auth (e.g. Basic dXNlcjpwYXNz...)"
                    class="input"
                />
            </div>
            <br/>
            <div class="filter-section mb-6 px-4">
                <label class="flex items-center space-x-2 text-sm text-gray-600 hover-effect p-2 rounded-lg">
                    <input
                        type="checkbox"
                        bind:checked={filterDateAndNull}
                        class="form-checkbox h-5 w-5 text-blue-600"
                    />
                    <span>Filter out Date fields and null values from response</span>
                </label>
            </div>

            <div class="section-header">
                <div class="section-icon">🔑</div>
                <label class="input-label">Parameters (Optional)</label>
            </div>
            <div class="pairs-container">
                {#each inputPairs as pair, i}
                    <div class="input-container mb-6 hover-effect">
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

            <div class="section-header">
                <div class="section-icon">📝</div>
                <label class="input-label">Scenario Details</label>
            </div>
            <div class="mb-4 px-4">
                <input
                    type="text"
                    bind:value={scenarioName}
                    class="input hover-effect"
                    placeholder="Enter scenario name"
                />
            </div>

            <div class="section-header">
                <div class="section-icon">📋</div>
                <label class="input-label">JSON Response</label>
            </div>
            <div class="mb-4 text-sm text-gray-600 px-4">
                Paste the JSON response from your API call here.
            </div>
            <textarea
                bind:value={inputText}
                class="input-textarea hover-effect"
                placeholder={exampleData}
            ></textarea>
        </div>

        <button 
            on:click={handleProcess}
            class="generate-button"
            disabled={!inputText || isProcessing}
        >
            <span class="button-text">{isProcessing ? 'Processing...' : 'Generate SpecFlow'}</span>
            {#if !isProcessing}
                <span class="button-icon">➜</span>
            {/if}
        </button>

        <div class="output-section">
            <div class="section-header">
                <div class="section-icon">✨</div>
                <label class="input-label">Generated SpecFlow</label>
            </div>
            <textarea
                readonly
                class="output-textarea hover-effect"
                value={outputText}
                placeholder="Generated SpecFlow will appear here..."
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

    .title-section {
        margin-bottom: 2rem;
        text-align: center;
    }

    .card-container {
        background-color: white;
        padding: 2rem;
        border-radius: 1.5rem;
        box-shadow: 0 4px 20px -1px rgba(0, 0, 0, 0.1), 0 2px 10px -1px rgba(0, 0, 0, 0.06);
        width: 90%;
        max-width: 1200px;
        transition: all 0.3s ease;
    }

    .card-container:hover {
        box-shadow: 0 8px 30px -1px rgba(0, 0, 0, 0.1), 0 4px 15px -1px rgba(0, 0, 0, 0.06);
    }

    .input-section, .output-section {
        margin-bottom: 2rem;
        padding: 0 2rem;
    }

    .section-header {
        display: flex;
        align-items: center;
        margin-bottom: 1.5rem;
        padding: 0.5rem 0;
        border-bottom: 2px solid #f0f0f0;
    }

    .section-icon {
        font-size: 1.5rem;
        margin-right: 0.75rem;
    }

    .input-label {
        font-size: 1.1rem;
        font-weight: 600;
        color: #4B5563;
        margin: 0;
    }

    .hover-effect {
        transition: all 0.2s ease;
    }

    .hover-effect:hover {
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
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
        border-radius: 0.75rem;
        font-size: 0.95rem;
        transition: all 0.2s;
        background-color: #FAFAFA;
    }

    .input:focus {
        outline: none;
        border-color: #60A5FA;
        box-shadow: 0 0 0 3px rgba(96, 165, 250, 0.2);
        background-color: white;
    }

    .input-textarea, .output-textarea {
        width: 100%;
        min-height: 200px;
        padding: 1rem;
        border: 1px solid #E5E7EB;
        border-radius: 0.75rem;
        font-family: monospace;
        font-size: 0.9rem;
        resize: vertical;
        background-color: #FAFAFA;
    }

    .input-textarea:focus, .output-textarea:focus {
        outline: none;
        border-color: #60A5FA;
        box-shadow: 0 0 0 3px rgba(96, 165, 250, 0.2);
        background-color: white;
    }

    .generate-button {
        width: calc(100% - 4rem);
        margin: 2rem 2rem;
        padding: 1rem;
        background: linear-gradient(135deg, #3B82F6 0%, #2563EB 100%);
        color: white;
        border: none;
        border-radius: 0.75rem;
        font-weight: 500;
        transition: all 0.3s ease;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
    }

    .generate-button:hover:not(:disabled) {
        transform: translateY(-2px);
        box-shadow: 0 4px 15px rgba(59, 130, 246, 0.4);
    }

    .generate-button:disabled {
        background: linear-gradient(135deg, #9CA3AF 0%, #6B7280 100%);
        cursor: not-allowed;
    }

    .button-text {
        font-size: 1.1rem;
    }

    .button-icon {
        font-size: 1.2rem;
        transition: transform 0.3s ease;
    }

    .generate-button:hover .button-icon {
        transform: translateX(5px);
    }

    .form-checkbox {
        border-radius: 0.25rem;
        border: 2px solid #D1D5DB;
        transition: all 0.2s ease;
    }

    .form-checkbox:checked {
        background-color: #3B82F6;
        border-color: #3B82F6;
        transform: scale(1.1);
    }

    .filter-section {
        background-color: #F9FAFB;
        border-radius: 0.75rem;
        transition: all 0.2s ease;
    }

    .filter-section:hover {
        background-color: #F3F4F6;
    }

    @keyframes gradient {
        0% { background-position: 0% 50%; }
        50% { background-position: 100% 50%; }
        100% { background-position: 0% 50%; }
    }

.pixelart-to-css {
  position: absolute;
  animation: x 2s infinite;
  -webkit-animation: x 2s infinite;
  -moz-animation: x 2s infinite;
  -o-animation: x 2s infinite;
}

@keyframes x {
0%, 33.3%{
  box-shadow: 10px 10px 0 0 rgba(255, 205, 210, 1), 20px 10px 0 0 rgba(255, 205, 210, 1), 30px 10px 0 0 rgba(255, 205, 210, 1), 40px 10px 0 0 rgba(255, 205, 210, 1), 50px 10px 0 0 rgba(255, 205, 210, 1), 60px 10px 0 0 rgba(255, 205, 210, 1), 70px 10px 0 0 rgba(255, 205, 210, 1), 80px 10px 0 0 rgba(255, 205, 210, 1), 90px 10px 0 0 rgba(255, 205, 210, 1), 100px 10px 0 0 rgba(255, 205, 210, 1), 110px 10px 0 0 rgba(255, 205, 210, 1), 120px 10px 0 0 rgba(255, 205, 210, 1), 130px 10px 0 0 rgba(255, 205, 210, 1), 140px 10px 0 0 rgba(255, 205, 210, 1), 150px 10px 0 0 rgba(255, 205, 210, 1), 160px 10px 0 0 rgba(255, 205, 210, 1), 170px 10px 0 0 rgba(255, 205, 210, 1), 180px 10px 0 0 rgba(255, 205, 210, 1), 190px 10px 0 0 rgba(255, 205, 210, 1), 200px 10px 0 0 rgba(255, 205, 210, 1), 210px 10px 0 0 rgba(255, 205, 210, 1), 10px 20px 0 0 rgba(255, 205, 210, 1), 20px 20px 0 0 rgba(255, 205, 210, 1), 30px 20px 0 0 rgba(255, 205, 210, 1), 40px 20px 0 0 rgba(255, 205, 210, 1), 50px 20px 0 0 rgba(255, 205, 210, 1), 60px 20px 0 0 rgba(255, 205, 210, 1), 70px 20px 0 0 rgba(255, 205, 210, 1), 80px 20px 0 0 rgba(255, 205, 210, 1), 90px 20px 0 0 rgba(255, 205, 210, 1), 100px 20px 0 0 rgba(255, 205, 210, 1), 110px 20px 0 0 rgba(255, 205, 210, 1), 120px 20px 0 0 rgba(255, 205, 210, 1), 130px 20px 0 0 rgba(255, 205, 210, 1), 140px 20px 0 0 rgba(255, 205, 210, 1), 150px 20px 0 0 rgba(255, 205, 210, 1), 160px 20px 0 0 rgba(255, 205, 210, 1), 170px 20px 0 0 rgba(255, 205, 210, 1), 180px 20px 0 0 rgba(255, 205, 210, 1), 190px 20px 0 0 rgba(255, 205, 210, 1), 200px 20px 0 0 rgba(255, 205, 210, 1), 210px 20px 0 0 rgba(255, 205, 210, 1), 10px 30px 0 0 rgba(255, 205, 210, 1), 20px 30px 0 0 rgba(255, 205, 210, 1), 30px 30px 0 0 rgba(255, 205, 210, 1), 40px 30px 0 0 rgba(255, 205, 210, 1), 50px 30px 0 0 rgba(255, 205, 210, 1), 60px 30px 0 0 rgba(255, 205, 210, 1), 70px 30px 0 0 rgba(255, 205, 210, 1), 80px 30px 0 0 rgba(255, 205, 210, 1), 90px 30px 0 0 rgba(255, 205, 210, 1), 100px 30px 0 0 rgba(255, 205, 210, 1), 110px 30px 0 0 rgba(255, 205, 210, 1), 120px 30px 0 0 rgba(255, 205, 210, 1), 130px 30px 0 0 rgba(255, 205, 210, 1), 140px 30px 0 0 rgba(255, 205, 210, 1), 150px 30px 0 0 rgba(255, 205, 210, 1), 160px 30px 0 0 rgba(255, 205, 210, 1), 170px 30px 0 0 rgba(255, 205, 210, 1), 180px 30px 0 0 rgba(255, 205, 210, 1), 190px 30px 0 0 rgba(255, 205, 210, 1), 200px 30px 0 0 rgba(255, 205, 210, 1), 210px 30px 0 0 rgba(255, 205, 210, 1);height: 10px; width: 10px;
  }
33.309999999999995%, 66.7%{
  box-shadow: 10px 10px 0 0 rgba(0, 188, 212, 1), 20px 10px 0 0 rgba(0, 188, 212, 1), 30px 10px 0 0 rgba(0, 188, 212, 1), 40px 10px 0 0 rgba(0, 188, 212, 1), 50px 10px 0 0 rgba(0, 188, 212, 1), 60px 10px 0 0 rgba(0, 188, 212, 1), 70px 10px 0 0 rgba(0, 188, 212, 1), 80px 10px 0 0 rgba(0, 188, 212, 1), 90px 10px 0 0 rgba(0, 188, 212, 1), 100px 10px 0 0 rgba(0, 188, 212, 1), 110px 10px 0 0 rgba(0, 188, 212, 1), 120px 10px 0 0 rgba(0, 188, 212, 1), 130px 10px 0 0 rgba(0, 188, 212, 1), 140px 10px 0 0 rgba(0, 188, 212, 1), 150px 10px 0 0 rgba(0, 188, 212, 1), 160px 10px 0 0 rgba(0, 188, 212, 1), 170px 10px 0 0 rgba(0, 188, 212, 1), 180px 10px 0 0 rgba(0, 188, 212, 1), 190px 10px 0 0 rgba(0, 188, 212, 1), 200px 10px 0 0 rgba(0, 188, 212, 1), 210px 10px 0 0 rgba(0, 188, 212, 1), 10px 20px 0 0 rgba(0, 188, 212, 1), 20px 20px 0 0 rgba(0, 188, 212, 1), 30px 20px 0 0 rgba(0, 188, 212, 1), 40px 20px 0 0 rgba(0, 188, 212, 1), 50px 20px 0 0 rgba(0, 188, 212, 1), 60px 20px 0 0 rgba(0, 188, 212, 1), 70px 20px 0 0 rgba(0, 188, 212, 1), 80px 20px 0 0 rgba(0, 188, 212, 1), 90px 20px 0 0 rgba(0, 188, 212, 1), 100px 20px 0 0 rgba(0, 188, 212, 1), 110px 20px 0 0 rgba(0, 188, 212, 1), 120px 20px 0 0 rgba(0, 188, 212, 1), 130px 20px 0 0 rgba(0, 188, 212, 1), 140px 20px 0 0 rgba(0, 188, 212, 1), 150px 20px 0 0 rgba(0, 188, 212, 1), 160px 20px 0 0 rgba(0, 188, 212, 1), 170px 20px 0 0 rgba(0, 188, 212, 1), 180px 20px 0 0 rgba(0, 188, 212, 1), 190px 20px 0 0 rgba(0, 188, 212, 1), 200px 20px 0 0 rgba(0, 188, 212, 1), 210px 20px 0 0 rgba(0, 188, 212, 1), 10px 30px 0 0 rgba(0, 188, 212, 1), 20px 30px 0 0 rgba(0, 188, 212, 1), 30px 30px 0 0 rgba(0, 188, 212, 1), 40px 30px 0 0 rgba(0, 188, 212, 1), 50px 30px 0 0 rgba(0, 188, 212, 1), 60px 30px 0 0 rgba(0, 188, 212, 1), 70px 30px 0 0 rgba(0, 188, 212, 1), 80px 30px 0 0 rgba(0, 188, 212, 1), 90px 30px 0 0 rgba(0, 188, 212, 1), 100px 30px 0 0 rgba(0, 188, 212, 1), 110px 30px 0 0 rgba(0, 188, 212, 1), 120px 30px 0 0 rgba(0, 188, 212, 1), 130px 30px 0 0 rgba(0, 188, 212, 1), 140px 30px 0 0 rgba(0, 188, 212, 1), 150px 30px 0 0 rgba(0, 188, 212, 1), 160px 30px 0 0 rgba(0, 188, 212, 1), 170px 30px 0 0 rgba(0, 188, 212, 1), 180px 30px 0 0 rgba(0, 188, 212, 1), 190px 30px 0 0 rgba(0, 188, 212, 1), 200px 30px 0 0 rgba(0, 188, 212, 1), 210px 30px 0 0 rgba(0, 188, 212, 1);height: 10px; width: 10px;
  }
66.71000000000001%, 100%{
  box-shadow: 10px 10px 0 0 rgba(255, 235, 59, 1), 20px 10px 0 0 rgba(255, 235, 59, 1), 30px 10px 0 0 rgba(255, 235, 59, 1), 40px 10px 0 0 rgba(255, 235, 59, 1), 50px 10px 0 0 rgba(255, 235, 59, 1), 60px 10px 0 0 rgba(255, 235, 59, 1), 70px 10px 0 0 rgba(255, 235, 59, 1), 80px 10px 0 0 rgba(255, 235, 59, 1), 90px 10px 0 0 rgba(255, 235, 59, 1), 100px 10px 0 0 rgba(255, 235, 59, 1), 110px 10px 0 0 rgba(255, 235, 59, 1), 120px 10px 0 0 rgba(255, 235, 59, 1), 130px 10px 0 0 rgba(255, 235, 59, 1), 140px 10px 0 0 rgba(255, 235, 59, 1), 150px 10px 0 0 rgba(255, 235, 59, 1), 160px 10px 0 0 rgba(255, 235, 59, 1), 170px 10px 0 0 rgba(255, 235, 59, 1), 180px 10px 0 0 rgba(255, 235, 59, 1), 190px 10px 0 0 rgba(255, 235, 59, 1), 200px 10px 0 0 rgba(255, 235, 59, 1), 210px 10px 0 0 rgba(255, 235, 59, 1), 10px 20px 0 0 rgba(255, 235, 59, 1), 20px 20px 0 0 rgba(255, 235, 59, 1), 30px 20px 0 0 rgba(255, 235, 59, 1), 40px 20px 0 0 rgba(255, 235, 59, 1), 50px 20px 0 0 rgba(255, 235, 59, 1), 60px 20px 0 0 rgba(255, 235, 59, 1), 70px 20px 0 0 rgba(255, 235, 59, 1), 80px 20px 0 0 rgba(255, 235, 59, 1), 90px 20px 0 0 rgba(255, 235, 59, 1), 100px 20px 0 0 rgba(255, 235, 59, 1), 110px 20px 0 0 rgba(255, 235, 59, 1), 120px 20px 0 0 rgba(255, 235, 59, 1), 130px 20px 0 0 rgba(255, 235, 59, 1), 140px 20px 0 0 rgba(255, 235, 59, 1), 150px 20px 0 0 rgba(255, 235, 59, 1), 160px 20px 0 0 rgba(255, 235, 59, 1), 170px 20px 0 0 rgba(255, 235, 59, 1), 180px 20px 0 0 rgba(255, 235, 59, 1), 190px 20px 0 0 rgba(255, 235, 59, 1), 200px 20px 0 0 rgba(255, 235, 59, 1), 210px 20px 0 0 rgba(255, 235, 59, 1), 10px 30px 0 0 rgba(255, 235, 59, 1), 20px 30px 0 0 rgba(255, 235, 59, 1), 30px 30px 0 0 rgba(255, 235, 59, 1), 40px 30px 0 0 rgba(255, 235, 59, 1), 50px 30px 0 0 rgba(255, 235, 59, 1), 60px 30px 0 0 rgba(255, 235, 59, 1), 70px 30px 0 0 rgba(255, 235, 59, 1), 80px 30px 0 0 rgba(255, 235, 59, 1), 90px 30px 0 0 rgba(255, 235, 59, 1), 100px 30px 0 0 rgba(255, 235, 59, 1), 110px 30px 0 0 rgba(255, 235, 59, 1), 120px 30px 0 0 rgba(255, 235, 59, 1), 130px 30px 0 0 rgba(255, 235, 59, 1), 140px 30px 0 0 rgba(255, 235, 59, 1), 150px 30px 0 0 rgba(255, 235, 59, 1), 160px 30px 0 0 rgba(255, 235, 59, 1), 170px 30px 0 0 rgba(255, 235, 59, 1), 180px 30px 0 0 rgba(255, 235, 59, 1), 190px 30px 0 0 rgba(255, 235, 59, 1), 200px 30px 0 0 rgba(255, 235, 59, 1), 210px 30px 0 0 rgba(255, 235, 59, 1);height: 10px; width: 10px;
  }
}
</style>