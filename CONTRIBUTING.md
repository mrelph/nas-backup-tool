# Contributing to NAS Backup Tool

Thank you for your interest in contributing to the NAS Backup Tool! We welcome contributions from the community.

## 🛠️ Development Environment

### Prerequisites
- **Windows 10/11** (for testing Windows Forms)
- **.NET 6.0 SDK** or later
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git** for version control

### Setup
1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/yourusername/nas-backup-tool.git
   ```
3. Create a development branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## 🎯 How to Contribute

### 🐛 Bug Reports
- Use the GitHub issue tracker
- Include detailed steps to reproduce
- Provide system information (Windows version, .NET version)
- Include screenshots if relevant

### 💡 Feature Requests
- Open an issue with the "enhancement" label
- Describe the use case and expected behavior
- Consider if it fits the project's scope

### 🔧 Code Contributions

#### Areas We Welcome Help
- **UI/UX Improvements**: Modern design enhancements
- **Performance Optimizations**: Faster file operations
- **Additional AI Models**: Support for more Bedrock models
- **Cross-platform Support**: Exploring .NET MAUI
- **Testing**: Unit tests and integration tests
- **Documentation**: Code comments and user guides

#### Code Style Guidelines
- Follow C# naming conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and concise
- Use async/await for I/O operations

#### UI Guidelines
- Maintain the dark theme consistency
- Use the established color palette
- Ensure accessibility (contrast, keyboard navigation)
- Test on different screen sizes
- Follow modern Windows design principles

## 📝 Pull Request Process

1. **Test Your Changes**
   - Build and run the application
   - Test core functionality (backup, duplicate detection)
   - Verify UI changes work correctly

2. **Update Documentation**
   - Update README.md if needed
   - Add XML comments for new public methods
   - Update user-facing documentation

3. **Submit Pull Request**
   - Provide clear description of changes
   - Reference related issues
   - Include screenshots for UI changes
   - Ensure all checks pass

4. **Code Review Process**
   - Maintainers will review your PR
   - Address feedback and make requested changes
   - Once approved, your PR will be merged

## 🧪 Testing

### Manual Testing Checklist
- [ ] Application builds without warnings
- [ ] UI loads correctly with dark theme
- [ ] Can add/remove source directories
- [ ] NAS connection test works
- [ ] Backup functionality operates correctly
- [ ] Duplicate detection finds duplicates
- [ ] AWS Bedrock integration (if configured)
- [ ] Scheduling features work
- [ ] Configuration saves/loads properly

### Future: Automated Testing
We're working on adding:
- Unit tests for core logic
- Integration tests for file operations
- UI automation tests

## 🔒 Security Guidelines

### Credential Handling
- Never log sensitive information
- Use Windows DPAPI for local storage
- Validate all user inputs
- Follow principle of least privilege

### File Operations
- Validate file paths to prevent directory traversal
- Handle file access exceptions gracefully
- Don't follow symbolic links outside source directories

## 🎨 Design Philosophy

### UI Principles
- **Modern**: Contemporary dark theme with subtle animations
- **Minimalist**: Clean interface without clutter
- **Intuitive**: Self-explanatory controls and workflows
- **Accessible**: Good contrast and keyboard navigation

### Code Principles
- **Secure by Default**: Encrypt sensitive data, validate inputs
- **Performance**: Async operations, efficient algorithms
- **Maintainable**: Clear structure, good separation of concerns
- **Extensible**: Plugin-friendly architecture for future features

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.

## 🙏 Recognition

Contributors will be acknowledged in:
- The README.md file
- Release notes for significant contributions
- GitHub contributors page

## 💬 Getting Help

- **Questions**: Open a GitHub discussion
- **Bug Reports**: Use GitHub issues
- **Feature Ideas**: Start with a GitHub discussion

Thank you for helping make the NAS Backup Tool better! 🚀