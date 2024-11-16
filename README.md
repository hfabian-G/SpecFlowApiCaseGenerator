# API to SpecFlow Converter

A tool that simplifies the creation of SpecFlow test cases by automatically converting API responses into formatted SpecFlow scenarios. Perfect for streamlining simple API testing workflows.

Please note that this implementation requires fairly specific SpecFlow step-definitions that are not provided with the project, though generating a few Scenarios should give you enough information to quickly replicate them. If need be, feel free to change the javascript in routes/+page.svelte to more precisely fit your current framework for API SpecFlow Scenario creation.

## Features
- One-click conversion of API responses to SpecFlow test scenarios
- Support for nested JSON objects and arrays
- Optional Basic Authentication support
- Clean, tabulated output format
- Automatic property validation generation

## Usage
1. Enter the base URL and endpoint of your API
2. Add Basic Authentication if required
3. Click Generate to create SpecFlow scenarios
4. Copy the generated output directly into your test files

## Technical Details
Built with SvelteKit and TailwindCSS.

## License
This project is licensed under Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)

You are free to:
- Share: Copy and redistribute the material in any medium or format
- Adapt: Remix, transform, and build upon the material

Under the following terms:
- Attribution: You must give appropriate credit
- NonCommercial: You may not use the material for commercial purposes
- No additional restrictions: You may not apply legal terms or technological measures that legally restrict others from doing anything the license permits

For the full license text, visit: https://creativecommons.org/licenses/by-nc/4.0/
