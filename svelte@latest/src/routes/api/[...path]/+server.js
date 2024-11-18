// src/routes/api/[...path]/+server.js
export async function GET({ params, url, request }) {
    try {
        const targetUrl = `http://vm-hsaws08d/${params.path}`;
        console.log('Proxying request to:', targetUrl);

        const headers = new Headers();
        headers.set('Content-Type', 'application/json');
        // Copy authorization header if present
        const authHeader = request.headers.get('Authorization');
        if (authHeader) {
            headers.set('Authorization', authHeader);
        }

        const response = await fetch(targetUrl, {
            method: 'GET',
            headers: headers,
            timeout: 30000 // 30 second timeout
        });

        const data = await response.json();
        return new Response(JSON.stringify(data), {
            headers: { 'Content-Type': 'application/json' }
        });
    } catch (error) {
        console.error('Proxy error:', error);
        return new Response(JSON.stringify({ error: error.message }), {
            status: 500,
            headers: { 'Content-Type': 'application/json' }
        });
    }
}

export const POST = GET; // Handle POST same way initially