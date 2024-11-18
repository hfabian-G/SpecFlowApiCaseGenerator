<script>
    let inputText = '';
    let outputText = '';
    let scenarioName = 'API Response';
    let isProcessing = false;

    function processResponse(jsonText, name = 'API Response') {
        try {
            const data = JSON.parse(jsonText);
            let output = '';

            // Build SpecFlow scenario
            output += `Scenario: ${name}\n`;
            output += `    Then the response should succeed\n`;

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

    // Example data
    const exampleData = `{
    "id": 1,
    "name": "Test User",
    "email": "test@example.com",
    "details": {
        "age": 30,
        "active": true
    },
    "roles": ["user", "admin"]
}`;
</script>

<div class="container bg-gradient-to-b from-gray-50 to-white">
    <h1 class="text-center text-3xl font-bold text-gray-800 mb-12">JSON Response to SpecFlow Converter</h1>
    
    <div class="card-container">
        <div class="input-section">
            <label class="input-label">JSON Response</label>
            <div class="mb-4 text-sm text-gray-600 px-4">
                Paste the JSON response from your API call here.
            </div>
            <div class="mb-4 px-4">
                <label class="block text-sm text-gray-600 mb-2">Scenario Name:</label>
                <input
                    type="text"
                    bind:value={scenarioName}
                    class="w-full p-2 border border-gray-300 rounded-md"
                    placeholder="Enter scenario name"
                />
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
            {isProcessing ? 'Processing...' : 'Generate SpecFlow'}
        </button>

        <div class="output-section">
            <label class="input-label">Generated SpecFlow</label>
            <textarea
                readonly
                class="output-textarea"
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