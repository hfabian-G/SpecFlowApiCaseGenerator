export async function GET({ params, url, request }) {
    const targetUrl = `http://vm-hsaws08d/${params.path}`;
    const headers = new Headers(request.headers);

    // Forward the request
    const response = await fetch(targetUrl, {
        method: request.method,
        headers: headers
    });

    return response;
}

export async function POST({ params, url, request }) {
    const targetUrl = `http://vm-hsaws08d/${params.path}`;
    const headers = new Headers(request.headers);
    const body = await request.text();

    // Forward the request
    const response = await fetch(targetUrl, {
        method: request.method,
        headers: headers,
        body: body
    });

    return response;
}