# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BlurBerry is a WinUI 3 media management application built on .NET 8 that allows users to import, organize, and manage image and video files. The application uses OpenCV for image processing capabilities and follows MVVM architecture patterns.

## Architecture

### Core Technologies
- **Framework**: WinUI 3 (Microsoft.WindowsAppSDK 1.7.250606001)
- **Runtime**: .NET 8.0 with Windows 10 API version 10.0.19041.0
- **UI Pattern**: MVVM using CommunityToolkit.Mvvm
- **Computer Vision**: OpenCvSharp4 (4.11.0.20250507)
- **Packaging**: MSIX with multi-architecture support (x86, x64, ARM64)

### Project Structure
```
BlurBerry/
├── Models/           # Data models (MediaInfo, MediaType enum)
├── ViewModels/       # MVVM ViewModels with singleton patterns
├── Views/            # UI components (Pages, UserControls, Windows)
├── Services/         # Business logic (MediaLibraryService)
├── Converters/       # Value converters for data binding
└── Assets/          # Application icons and images
```

### Key Architectural Patterns
- **Singleton Pattern**: ViewModels and Services use static Instance properties
- **Observable Collections**: MediaInfos and FilteredMediaInfos for UI binding
- **Command Pattern**: RelayCommand for UI interactions
- **Repository Pattern**: MediaLibraryService handles data persistence

## Development Commands

### Build
```bash
dotnet build BlurBerry/BlurBerry.csproj
```

### Run Application
```bash
dotnet run --project BlurBerry/BlurBerry.csproj
```

### Publish (Platform-specific)
```bash
# Windows x64
dotnet publish BlurBerry/BlurBerry.csproj -r win-x64 -c Release

# Windows x86
dotnet publish BlurBerry/BlurBerry.csproj -r win-x86 -c Release

# Windows ARM64
dotnet publish BlurBerry/BlurBerry.csproj -r win-arm64 -c Release
```

## Core Components

### MediaInfo Model
Central data model representing media files with properties:
- File metadata (path, size, dates)
- Media type classification (Image/Video)
- Thumbnail management and caching
- Selection state for UI operations

### MediaLibraryService
Singleton service managing:
- JSON-based library persistence (`media_library.json`)
- Thumbnail generation and caching in `ThumbnailCache` folder
- Async file operations with error handling
- Library cleanup and orphaned file management

### HomePageViewModel
Main application logic handling:
- Media import from files/folders with file type filtering
- Media filtering by type (All/Image/Video)
- Multi-selection operations with batch actions
- Real-time UI state management for selection feedback

## UI Architecture

### Navigation Structure
- **MainWindow**: NavigationView with Mica backdrop and custom TitleBar
- **HomePage**: Primary interface with media grid and filtering controls
- **UserControls**: Reusable components (MediaItemControl, various panels)

### XAML Patterns
- Resource dictionaries for value converters in App.xaml
- Data binding with Mode specifications and UpdateSourceTrigger
- Custom control templates with VisualStateManager for interactions
- Grid layouts with responsive column/row definitions

## File Handling

### Supported Formats
- **Images**: .jpg, .jpeg, .png
- **Videos**: .mp4, .avi, .mkv

### File Operations
- Drag-and-drop support through FileOpenPicker and FolderPicker
- Duplicate detection based on file path and modification time
- Thumbnail generation using Windows Storage APIs (ThumbnailMode.PicturesView, 200px)
- Automatic cleanup of orphaned thumbnails on application startup

## State Management

### Data Flow
1. MediaLibraryService loads persisted library on startup
2. HomePageViewModel maintains observable collections for UI binding
3. Filter operations update FilteredMediaInfos collection
4. Selection changes trigger property notifications for UI updates
5. All modifications persist automatically via MediaLibraryService

### Error Handling
- Extensive try-catch blocks with Debug.WriteLine for diagnostics
- Graceful fallback for missing files and failed operations
- Null-safe operations throughout the codebase with nullable reference types

## Localization
The application uses Korean language strings throughout the UI. When adding new features, maintain consistency with existing Korean localization patterns.