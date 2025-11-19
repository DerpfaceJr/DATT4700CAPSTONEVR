# Script to connect local repository to GitHub remote
# Run this after creating the repository on GitHub

$repoName = "DATT4700CAPSTONEVR"
$gitHubUsername = Read-Host "Enter your GitHub username"

Write-Host "Setting up remote repository..."
git remote add origin "https://github.com/$gitHubUsername/$repoName.git"
git branch -M main
git push -u origin main

Write-Host "Repository successfully pushed to GitHub!"
Write-Host "Repository URL: https://github.com/$gitHubUsername/$repoName"

