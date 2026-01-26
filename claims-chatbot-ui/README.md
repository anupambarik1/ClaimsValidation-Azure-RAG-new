# Claims Chatbot UI

Angular-based chatbot interface for the Claims RAG Bot API.

## Features

- 💬 **Interactive Chat Interface** - Conversational UI for claim validation
- 📄 **Document Upload** - Drag-and-drop support for claim documents (PDF, JPG, PNG)
- 📝 **Claim Form** - Manual claim entry with validation
- 🤖 **AI-Powered Extraction** - Automatic claim data extraction from documents
- 📊 **Confidence Scoring** - Visual confidence indicators for extracted data
- 🎨 **Material Design** - Modern, responsive UI with Angular Material
- 🔄 **Real-time Updates** - Live chat updates and progress indicators

## Prerequisites

- Node.js 18+ and npm
- Angular CLI
- .NET Claims RAG Bot API running (default: https://localhost:5001)

## Installation

```powershell
# Install dependencies
npm install

# Start development server
npm start
```

The application will be available at `http://localhost:4200`

## Configuration

Update API endpoint in `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:5001/api'  // Your API URL
};
```

## Usage

### Upload & Process Documents

1. Click on **Upload Document** tab
2. Drag & drop or select a claim document
3. Choose document type (Claim Form, Police Report, etc.)
4. Click **Upload & Process**
5. View extracted claim data with confidence scores

### Manual Claim Validation

1. Click on **Claim Form** tab
2. Fill in policy details:
   - Policy Number
   - Policy Type (Motor, Home, Health, Life)
   - Claim Amount
   - Claim Description
3. Click **Validate Claim**
4. View AI-powered validation result

### Chat Interactions

- Type questions in the text input
- View claim results in the chat history
- Clear chat to start fresh

## Project Structure

```
src/
├── app/
│   ├── components/
│   │   ├── chat/                 # Main chat interface
│   │   ├── claim-form/           # Manual claim entry form
│   │   ├── claim-result/         # Result display component
│   │   └── document-upload/      # Document upload with drag-drop
│   ├── models/
│   │   └── claim.model.ts        # TypeScript interfaces
│   ├── services/
│   │   ├── chat.service.ts       # Chat message management
│   │   └── claims-api.service.ts # API integration
│   ├── app.component.ts          # Root component
│   └── app.config.ts             # App configuration
├── environments/                  # Environment configs
└── styles.scss                    # Global styles
```

## API Integration

The UI integrates with these .NET API endpoints:

- `POST /api/claims/validate` - Validate claim with RAG
- `POST /api/documents/submit` - Upload and extract claim data
- `POST /api/documents/upload` - Upload document only
- `POST /api/documents/extract` - Extract from uploaded document
- `DELETE /api/documents/{id}` - Delete document

## Development

```powershell
# Serve with live reload
npm start

# Build for production
npm run build

# Run tests
npm test

# Watch mode (rebuild on file changes)
npm run watch
```

## Build

```powershell
npm run build
```

Production files will be in `dist/claims-chatbot-ui/`

## Deployment

### Local IIS / Windows

1. Build the application
2. Copy `dist/claims-chatbot-ui/browser` contents to IIS web folder
3. Configure URL rewrite to support Angular routing

### With .NET API

Serve static files from the .NET API project:

1. Build Angular app
2. Copy dist files to `src/ClaimsRagBot.Api/wwwroot`
3. Configure .NET to serve static files

## Troubleshooting

### CORS Issues

If you see CORS errors, ensure the .NET API has CORS configured:

```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAngular", policy => {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### API Connection Refused

- Verify .NET API is running
- Check API URL in `environment.ts`
- Verify SSL certificate if using HTTPS

### File Upload Fails

- Check file size (max 10MB)
- Verify file type (PDF, JPG, PNG only)
- Ensure S3 bucket is configured in API

## Technologies

- Angular 18 (Standalone Components)
- Angular Material 18
- RxJS 7
- TypeScript 5.5
- SCSS

## License

Private - NGA AAP Claims Autobot Project
