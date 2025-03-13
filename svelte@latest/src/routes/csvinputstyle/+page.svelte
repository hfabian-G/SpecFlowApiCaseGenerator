<script>
    import { onMount } from 'svelte';
    import Papa from 'papaparse';

    let scenarios = [
        {
            name: '',
            auth: '',
            url: '',
            parameters: [{ key: '', value: '' }]
        }
    ];
    let outputText = '';
    let isProcessing = false;
    let useBodyParams = false;
    let showTemplates = false;
    let selectedTemplate = '';

    const templates = {
        'Get Users': {
            name: 'Get Users List',
            auth: '',
            url: 'https://api.example.com/users',
            parameters: [
                { key: 'limit', value: '10' },
                { key: 'offset', value: '0' }
            ]
        },
        'Pokemon API': {
            name: 'Get Pokemon List',
            auth: '',
            url: 'https://pokeapi.co/api/v2/pokemon',
            parameters: [
                { key: 'limit', value: '5' }
            ]
        }
    };

    function addScenario() {
        scenarios = [...scenarios, {
            name: '',
            auth: '',
            url: '',
            parameters: [{ key: '', value: '' }]
        }];
    }

    function removeScenario(index) {
        scenarios = scenarios.filter((_, i) => i !== index);
    }

    function addParameter(scenarioIndex) {
        scenarios = scenarios.map((scenario, i) => {
            if (i === scenarioIndex) {
                return {
                    ...scenario,
                    parameters: [...scenario.parameters, { key: '', value: '' }]
                };
            }
            return scenario;
        });
    }

    function removeParameter(scenarioIndex, paramIndex) {
        scenarios = scenarios.map((scenario, i) => {
            if (i === scenarioIndex) {
                return {
                    ...scenario,
                    parameters: scenario.parameters.filter((_, j) => j !== paramIndex)
                };
            }
            return scenario;
        });
    }

    function duplicateScenario(index) {
        const scenarioToDuplicate = JSON.parse(JSON.stringify(scenarios[index]));
        scenarioToDuplicate.name += ' (Copy)';
        scenarios = [...scenarios.slice(0, index + 1), scenarioToDuplicate, ...scenarios.slice(index + 1)];
    }

    function applyTemplate(templateName) {
        if (templates[templateName]) {
            scenarios = [JSON.parse(JSON.stringify(templates[templateName]))];
            showTemplates = false;
        }
    }

    function clearAll() {
        scenarios = [{
            name: '',
            auth: '',
            url: '',
            parameters: [{ key: '', value: '' }]
        }];
        outputText = '';
    }

    async function validateEndpoint(url, auth = '', params = null, useBody = false) {
        try {
            const headers = {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
            };
            
            if (auth) {
                headers['Authorization'] = auth;
            }

            let finalUrl = url;
            const options = {
                method: useBody ? 'POST' : 'GET',
                headers: headers
            };

            if (params) {
                if (useBody) {
                    options.body = JSON.stringify(params);
                } else {
                    const queryString = new URLSearchParams(params).toString();
                    finalUrl = `${url}?${queryString}`;
                }
            }

            const response = await fetch(finalUrl, options);
            const data = await response.json();
            return { response, data };
        } catch (error) {
            console.error('Fetch error:', error);
            throw error;
        }
    }

    async function handleProcess() {
        isProcessing = true;
        outputText = '';
        
        try {
            let output = '';
            let previousAuth = '';

            for (const [index, scenario] of scenarios.entries()) {
                if (!scenario.url) continue;

                try {
                    const auth = scenario.auth === 'prev' ? previousAuth : scenario.auth;
                    
                    if (index === 0 && auth === 'prev') {
                        output += 'Error: First scenario cannot use "prev" as Authorization\n\n';
                        continue;
                    }

                    if (scenario.auth !== 'prev') {
                        previousAuth = scenario.auth;
                    }

                    // Convert parameters array to object
                    const params = {};
                    scenario.parameters.forEach(param => {
                        if (param.key && param.value) {
                            params[param.key] = param.value;
                        }
                    });

                    const { response, data } = await validateEndpoint(scenario.url, auth, params, useBodyParams);
                    
                    output += `Scenario: ${scenario.name || `Scenario ${index + 1}`}\n`;
                    output += `    Given an API route "${scenario.url}"\n`;
                    
                    if (auth) {
                        output += `    And I make a request with the authorization "${auth}"\n`;
                    }

                    if (Object.keys(params).length > 0) {
                        if (useBodyParams) {
                            output += `    And request body\n`;
                            output += `      """\n`;
                            output += `      ${JSON.stringify(params, null, 6).replace(/^/gm, '      ')}\n`;
                            output += `      """\n`;
                        } else {
                            output += `    Given the parameters\n`;
                            output += `      | name | value |\n`;
                            Object.entries(params).forEach(([key, value]) => {
                                output += `      | ${key} | ${value} |\n`;
                            });
                        }
                    }

                    output += `    When I validate the response\n`;

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
                                
                                // Create a map of required properties for list items
                                const requiredProps = new Set();
                                value.forEach(item => {
                                    if (typeof item === 'object' && item !== null) {
                                        Object.keys(item).forEach(prop => requiredProps.add(prop));
                                    }
                                });

                                // For each required property, validate it exists in each item
                                requiredProps.forEach(prop => {
                                    output += `    And each item in ${propertyPath} should have property "${prop}"\n`;
                                });

                                // Process the first item's properties as an example
                                if (value.length > 0 && typeof value[0] === 'object' && value[0] !== null) {
                                    processProperties(value[0], `${propertyPath}[*]`);
                                }
                            } else if (typeof value === 'object' && value !== null) {
                                processProperties(value, propertyPath);
                            } else {
                                output += `    And property ${propertyPath} should be "${value}"\n`;
                            }
                        }
                    }

                    processProperties(data);
                    output += '\n';

                } catch (error) {
                    output += `Error processing scenario ${index + 1}: ${error.message}\n\n`;
                }
            }

            outputText = output || 'No valid scenarios to process';
        } catch (error) {
            outputText = `Error: ${error.message}`;
        } finally {
            isProcessing = false;
        }
    }
</script>

<div class="container">
    <h1>Batch API to SpecFlow Converter</h1>
    <p class="subtitle">Create multiple scenarios with an intuitive spreadsheet interface</p>
    
    <div class="card-container">
        <div class="toolbar">
            <button class="tool-button" on:click={() => showTemplates = !showTemplates}>
                <span class="button-icon">📋</span>
                Templates
            </button>
            <button class="tool-button" on:click={addScenario}>
                <span class="button-icon">➕</span>
                Add Scenario
            </button>
            <button class="tool-button" on:click={clearAll}>
                <span class="button-icon">🗑️</span>
                Clear All
            </button>
            <div class="checkbox-container">
                <label class="checkbox-label">
                    <input
                        type="checkbox"
                        bind:checked={useBodyParams}
                        class="form-checkbox"
                    />
                    <span>Send parameters in request body (POST)</span>
                </label>
            </div>
        </div>

        {#if showTemplates}
            <div class="templates-panel">
                <div class="section-header">
                    <div class="section-icon">📋</div>
                    <label class="input-label">Templates</label>
                </div>
                <div class="templates-grid">
                    {#each Object.entries(templates) as [name, template]}
                        <button 
                            class="template-card"
                            on:click={() => applyTemplate(name)}
                        >
                            <h3>{name}</h3>
                            <p class="template-url">{template.url}</p>
                            <p class="template-params">
                                {template.parameters.length} parameter{template.parameters.length !== 1 ? 's' : ''}
                            </p>
                        </button>
                    {/each}
                </div>
            </div>
        {/if}

        <div class="scenarios-container">
            {#each scenarios as scenario, scenarioIndex}
                <div class="scenario-card">
                    <div class="scenario-header">
                        <div class="scenario-number">#{scenarioIndex + 1}</div>
                        <input
                            type="text"
                            bind:value={scenario.name}
                            placeholder="Scenario Name"
                            class="scenario-name-input"
                        />
                        <div class="scenario-actions">
                            <button 
                                class="icon-button"
                                on:click={() => duplicateScenario(scenarioIndex)}
                                title="Duplicate Scenario"
                            >
                                📋
                            </button>
                            {#if scenarios.length > 1}
                                <button 
                                    class="icon-button"
                                    on:click={() => removeScenario(scenarioIndex)}
                                    title="Remove Scenario"
                                >
                                    ❌
                                </button>
                            {/if}
                        </div>
                    </div>

                    <div class="scenario-body">
                        <div class="input-row">
                            <label class="row-label">URL</label>
                            <input
                                type="text"
                                bind:value={scenario.url}
                                placeholder="https://api.example.com/endpoint"
                                class="url-input"
                            />
                        </div>

                        <div class="input-row">
                            <label class="row-label">Auth</label>
                            <input
                                type="text"
                                bind:value={scenario.auth}
                                placeholder="Authorization (or 'prev' to reuse previous)"
                                class="auth-input"
                            />
                        </div>

                        <div class="parameters-section">
                            <div class="parameters-header">
                                <label class="row-label">Parameters</label>
                                <button 
                                    class="add-param-button"
                                    on:click={() => addParameter(scenarioIndex)}
                                >
                                    Add Parameter
                                </button>
                            </div>
                            <div class="parameters-grid">
                                <div class="param-header">Key</div>
                                <div class="param-header">Value</div>
                                <div class="param-header"></div>
                                {#each scenario.parameters as param, paramIndex}
                                    <input
                                        type="text"
                                        bind:value={param.key}
                                        placeholder="Parameter name"
                                        class="param-input"
                                    />
                                    <input
                                        type="text"
                                        bind:value={param.value}
                                        placeholder="Parameter value"
                                        class="param-input"
                                    />
                                    <button 
                                        class="remove-param-button"
                                        on:click={() => removeParameter(scenarioIndex, paramIndex)}
                                        disabled={scenario.parameters.length === 1}
                                    >
                                        ✕
                                    </button>
                                {/each}
                            </div>
                        </div>
                    </div>
                </div>
            {/each}
        </div>

        <button 
            on:click={handleProcess}
            class="generate-button"
            disabled={!scenarios.some(s => s.url) || isProcessing}
        >
            <span class="button-text">
                {isProcessing ? 'Processing...' : 'Generate SpecFlow Scenarios'}
            </span>
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
        background: linear-gradient(135deg, #ffffff 0%, #f0f0f0 100%);
    }

    h1 {
        font-size: 2.4rem;
        font-family: var(--font-display);
        font-weight: 400;
        color: #000000;
        margin-bottom: 1rem;
        text-transform: uppercase;
        letter-spacing: 0.1em;
        text-align: center;
    }

    .subtitle {
        color: #404040;
        font-size: 1.1rem;
        font-family: var(--font-body);
        font-weight: 400;
        text-align: center;
        margin-bottom: 2rem;
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

    .toolbar {
        display: flex;
        gap: 1rem;
        align-items: center;
        margin-bottom: 2rem;
        padding: 1rem;
        background-color: #f8f8f8;
        border-radius: 0.75rem;
        border: 2px solid #000000;
    }

    .tool-button {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 1rem;
        background-color: #ffffff;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        font-family: var(--font-display);
        font-size: 0.9rem;
        color: #000000;
        cursor: pointer;
        transition: all 0.2s ease;
    }

    .tool-button:hover {
        transform: translate(-2px, -2px);
        box-shadow: 2px 2px 0 #000000;
    }

    .templates-panel {
        margin-bottom: 2rem;
        padding: 1.5rem;
        background-color: #f8f8f8;
        border-radius: 0.75rem;
        border: 2px solid #000000;
    }

    .templates-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 1rem;
        margin-top: 1rem;
    }

    .template-card {
        padding: 1rem;
        background-color: #ffffff;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        text-align: left;
        cursor: pointer;
        transition: all 0.2s ease;
    }

    .template-card:hover {
        transform: translate(-2px, -2px);
        box-shadow: 2px 2px 0 #000000;
    }

    .template-card h3 {
        font-family: var(--font-display);
        font-size: 1.1rem;
        margin: 0 0 0.5rem 0;
    }

    .template-url {
        font-family: var(--font-mono);
        font-size: 0.8rem;
        color: #666;
        margin: 0 0 0.5rem 0;
    }

    .template-params {
        font-size: 0.9rem;
        color: #666;
        margin: 0;
    }

    .scenarios-container {
        display: flex;
        flex-direction: column;
        gap: 2rem;
    }

    .scenario-card {
        background-color: #ffffff;
        border: 2px solid #000000;
        border-radius: 0.75rem;
        overflow: hidden;
    }

    .scenario-header {
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 1rem;
        background-color: #f8f8f8;
        border-bottom: 2px solid #000000;
    }

    .scenario-number {
        font-family: var(--font-display);
        font-size: 1.2rem;
        font-weight: 400;
        color: #000000;
    }

    .scenario-name-input {
        flex: 1;
        padding: 0.5rem;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        font-family: var(--font-display);
        font-size: 1rem;
        background-color: #ffffff;
    }

    .scenario-actions {
        display: flex;
        gap: 0.5rem;
    }

    .icon-button {
        padding: 0.5rem;
        background: none;
        border: none;
        cursor: pointer;
        font-size: 1.2rem;
        transition: transform 0.2s ease;
    }

    .icon-button:hover {
        transform: scale(1.1);
    }

    .scenario-body {
        padding: 1.5rem;
    }

    .input-row {
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-bottom: 1rem;
    }

    .row-label {
        min-width: 80px;
        font-family: var(--font-display);
        font-size: 0.9rem;
        color: #000000;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }

    .url-input, .auth-input {
        flex: 1;
        padding: 0.75rem;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        font-family: var(--font-body);
        font-size: 0.95rem;
        background-color: #ffffff;
    }

    .parameters-section {
        margin-top: 1.5rem;
    }

    .parameters-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1rem;
    }

    .add-param-button {
        padding: 0.5rem 1rem;
        background-color: #ffffff;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        font-family: var(--font-display);
        font-size: 0.9rem;
        color: #000000;
        cursor: pointer;
        transition: all 0.2s ease;
    }

    .add-param-button:hover {
        transform: translate(-2px, -2px);
        box-shadow: 2px 2px 0 #000000;
    }

    .parameters-grid {
        display: grid;
        grid-template-columns: 1fr 1fr auto;
        gap: 0.75rem;
        align-items: center;
    }

    .param-header {
        font-family: var(--font-display);
        font-size: 0.8rem;
        color: #666;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }

    .param-input {
        padding: 0.5rem;
        border: 2px solid #000000;
        border-radius: 0.5rem;
        font-family: var(--font-body);
        font-size: 0.9rem;
        background-color: #ffffff;
    }

    .remove-param-button {
        padding: 0.5rem;
        background: none;
        border: none;
        cursor: pointer;
        font-size: 1rem;
        color: #666;
        opacity: 0.5;
        transition: all 0.2s ease;
    }

    .remove-param-button:not(:disabled):hover {
        opacity: 1;
        transform: scale(1.1);
    }

    .remove-param-button:disabled {
        cursor: not-allowed;
    }

    .generate-button {
        width: 100%;
        padding: 1rem;
        margin: 2rem 0;
        background-color: #000000;
        color: white;
        border: 2px solid #000000;
        border-radius: 0.75rem;
        font-family: var(--font-display);
        font-size: 1.2rem;
        font-weight: 400;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        transition: all 0.2s ease;
        text-transform: uppercase;
        letter-spacing: 0.1em;
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

    .input-label {
        font-size: 1.2rem;
        font-family: var(--font-display);
        font-weight: 400;
        color: #000000;
        margin: 0;
        text-transform: uppercase;
        letter-spacing: 0.1em;
    }

    .output-textarea {
        width: 100%;
        min-height: 300px;
        padding: 1rem;
        border: 2px solid #000000;
        border-radius: 0.75rem;
        font-family: var(--font-mono);
        font-size: 0.9rem;
        background-color: #ffffff;
        resize: vertical;
    }

    .output-textarea:focus {
        outline: none;
        border-color: #000000;
        box-shadow: 4px 4px 0 rgba(0, 0, 0, 0.2);
        transform: translate(-2px, -2px);
    }

    .checkbox-container {
        margin-left: auto;
        padding: 0.5rem 1rem;
        background-color: #ffffff;
        border: 2px solid #000000;
        border-radius: 0.5rem;
    }

    .checkbox-label {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        font-family: var(--font-body);
        font-size: 0.9rem;
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
</style>