import { HttpEvent, HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay, tap } from 'rxjs/operators';

export function mockDataInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> {
  // Check if we are in "Demo Mode"
  // For now, we'll assume if the hostname is github.io, we are in demo mode.
  // We can also add a query param check like ?demo=true
  const isDemoMode = window.location.hostname.includes('github.io') || window.location.search.includes('demo=true');

  if (!isDemoMode) {
    return next(req);
  }

  // Intercept Login
  if (req.method === 'POST' && req.url.toLowerCase().includes('/authentication/login')) {
    console.log('[Demo Mode] Intercepting Login');
    const dummyToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkRlbW8gQWRtaW4iLCJlbWFpbCI6ImRlbW9AYWRtaW4uY29tIiwicm9sZSI6IkFkbWluIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';
    return of(new HttpResponse({ status: 200, body: { token: dummyToken } })).pipe(delay(500));
  }

  // Only intercept GET requests for data
  if (req.method !== 'GET') {
    return next(req);
  }

  // Map API endpoints to JSON files
  const urlLower = req.url.toLowerCase();
  let jsonFile = '';

  if (urlLower.includes('/api/companies')) {
    jsonFile = 'assets/mock-data/Companies.json';
  } else if (urlLower.includes('/api/warehouses')) {
    jsonFile = 'assets/mock-data/Warehouses.json';
  } else if (urlLower.includes('/api/materials')) {
    jsonFile = 'assets/mock-data/Materials.json';
  } else if (urlLower.includes('/api/locations')) {
    jsonFile = 'assets/mock-data/Locations.json';
  } else if (urlLower.includes('/api/pallets')) {
    jsonFile = 'assets/mock-data/Pallets.json';
  } else if (urlLower.includes('/api/docks')) {
    jsonFile = 'assets/mock-data/Docks.json';
  } else if (urlLower.includes('/api/yardspots')) {
    jsonFile = 'assets/mock-data/YardSpots.json';
  }

  if (jsonFile) {
    console.log(`[Demo Mode] Intercepting ${req.url}, serving ${jsonFile}`);
    
    // We need to fetch the JSON file using a standard fetch or XHR, 
    // but since we are in an interceptor, we can't easily use HttpClient to fetch the JSON 
    // without triggering the interceptor again (unless we are careful).
    // However, since the JSON file is an asset, it's just another HTTP request.
    // To avoid infinite loops, we can check if the request is already for an asset.
    if (req.url.includes('assets/')) {
        return next(req);
    }

    // Create a new request for the JSON file
    const jsonReq = req.clone({
      url: jsonFile,
      method: 'GET'
    });

    return next(jsonReq);
  }

  return next(req);
}
