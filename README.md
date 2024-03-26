# NMP Frontend
This directory contains the frontend code for the NMP project.

## Main Branches:

- main: This branch represents the stable production-ready code. It should ideally reflect the code currently in production.
- develop: This branch serves as the integration branch for ongoing development. It's where feature branches are merged for testing before being merged into master/main.
  
## Feature Branches:

feature/* or feat/*: Feature branches are created for each new feature or significant change. They branch off from develop and are merged back into develop once the feature is complete.

## Release Branches:

release/*: Release branches are created when preparing for a new production release. They branch off from develop and are used for final testing and bug fixes. Once ready, they are merged into both master/main and develop.

## Hotfix Branches:

hotfix/*: Hotfix branches are used to address critical issues in production. They branch off from master/main, contain the fix, and are merged back into both master/main and develop.
## Support/Branch for Legacy Versions:

support/*: If you need to maintain multiple versions of your application, you may have support branches for each version. These branches can be used to apply critical bug fixes to specific versions.

## Experimental Branches:

experiment/* or dev/*: for experimental features or ongoing development that's not yet ready for integration into develop.
