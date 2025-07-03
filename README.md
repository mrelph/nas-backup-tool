# 🗂️ NAS Backup Tool

A modern Windows desktop application for backing up files from your PC to a NAS (Network Attached Storage) server with AI-powered duplicate detection and intelligent file management.

![NAS Backup Tool](https://img.shields.io/badge/.NET-6.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## ✨ Features

### 🎨 Modern UI
- **Dark Theme**: Sleek modern interface with cool blue and teal accents
- **Intuitive Design**: Clean, minimalist layout for easy navigation
- **Real-time Feedback**: Progress tracking and detailed logging

### 📁 Advanced Backup
- **Multiple Sources**: Select multiple directories to backup simultaneously
- **Incremental Backup**: Only copies changed or new files to save time
- **NAS Integration**: Full support for network attached storage with authentication
- **Scheduling**: Automated backups on selected days and times

### 🤖 AI-Powered Intelligence
- **Amazon Bedrock Integration**: Uses Claude AI models for intelligent file analysis
- **Duplicate Detection**: Automatically finds and analyzes duplicate files
- **Smart Recommendations**: AI suggests which duplicates to keep or remove
- **Space Optimization**: Saves storage by identifying redundant files

### 🔒 Security
- **Encrypted Storage**: AWS credentials and passwords secured with Windows DPAPI
- **Secure Transmission**: All network communication uses standard secure protocols
- **Per-user Isolation**: Credentials cannot be accessed by other users

## 🛠️ Requirements

- **OS**: Windows 10 or later
- **Runtime**: .NET 6.0 or later
- **Network**: Access to your NAS server
- **Optional**: AWS account for AI features (Amazon Bedrock)

## 🚀 Quick Start

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/nas-backup-tool.git
   cd nas-backup-tool
   ```

2. **Build the application**:
   ```bash
   dotnet build --configuration Release
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```

### Alternative: Self-contained Build
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 📖 Usage Guide

### 🏗️ Initial Setup

1. **Launch** the application
2. **Navigate** to the ⚙️ **Settings** tab
3. **Configure NAS Connection**:
   - Enter your NAS server address (e.g., `\\192.168.1.100`)
   - Enable authentication if required
   - Test the connection

4. **Optional: Configure AI Features**:
   - Add your AWS Access Key and Secret Key
   - Select your preferred AWS region
   - Choose a Bedrock model (Claude 3 recommended)

### 💾 Creating Backups

1. **Go to** 🗂️ **Backup** tab
2. **Add source directories** using the "Add Folder" button
3. **Set destination path** on your NAS
4. **Start backup** and monitor progress
5. **Review logs** for detailed information

### 🔍 Duplicate Analysis

1. **Navigate to** 🔍 **Duplicates** tab
2. **Click "Analyze Duplicates"** to scan your source directories
3. **Review results** showing duplicate files and wasted space
4. **Get AI recommendations** for which files to keep
5. **Remove duplicates** to optimize storage

### ⏰ Scheduling

1. **Go to** ⏰ **Schedule** tab
2. **Enable scheduled backup**
3. **Set time and days** for automatic backups
4. **Application runs backups** in the background

## 🏗️ Architecture

### Core Components

- **MainForm.cs**: Modern UI with dark theme and tabbed interface
- **BackupEngine.cs**: File copying and synchronization logic
- **DuplicateAnalyzer.cs**: AI-powered duplicate detection and analysis
- **BackupConfig.cs**: Configuration management with secure credential storage

### AI Integration

- **Amazon Bedrock**: Claude AI models for intelligent file analysis
- **Supported Models**:
  - Claude 3 Haiku (Fast, cost-effective)
  - Claude 3 Sonnet (Balanced performance)
  - Claude 3 Opus (Highest capability)
  - Claude v2.1 (Previous generation)

## 📁 Configuration

### Settings Location
- **Config File**: `%APPDATA%\NASBackup\config.json`
- **Secure Credentials**: `%APPDATA%\NASBackup\credentials.dat` (encrypted)

### NAS Path Examples
- **IP Address**: `\\192.168.1.100\backup`
- **Hostname**: `\\mynas.local\shared\backup`
- **FQDN**: `\\nas.company.com\backups\username`

## 🔧 Troubleshooting

### ❌ Connection Issues
- Verify NAS is powered on and accessible
- Test path in Windows Explorer first
- Check network connectivity and firewall settings
- Ensure correct credentials if authentication is required

### ⚠️ Permission Issues
- Verify write permissions to destination folder
- Check if NAS requires specific user accounts
- Ensure source files are not locked by other applications

### 🤖 AI Features Not Working
- Verify AWS credentials are correct
- Check AWS region matches your Bedrock access
- Ensure selected model is available in your region
- Review AWS CloudWatch logs for detailed errors

## 🔒 Security Considerations

- **Credential Storage**: Uses Windows DPAPI for local encryption
- **Network Security**: Relies on standard Windows file sharing security
- **AWS Integration**: Credentials stored locally, not transmitted to third parties
- **Data Privacy**: No file content is sent to AWS (only metadata for analysis)

## 🚀 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with .NET 6.0 and Windows Forms
- AI powered by Amazon Bedrock and Claude models
- Modern UI inspired by contemporary design principles
- Secure credential storage using Windows DPAPI

---

**Made with ❤️ for efficient and intelligent backup management**