# git hooks

This folder contains two git hooks to be executed before commiting a change to the repo:

* `remove-trailing-whitespaces-before-commit` which removes the trailing whitespaces and,
* `dotnet-build-before-commit` which builds the project with the new changes in a temporary location

## Install

```bash
$ cd Build/git-hooks
$ sudo bash install-hooks.sh
```

It requires `sudo` because it has to grant execution permissions to the bash files.
