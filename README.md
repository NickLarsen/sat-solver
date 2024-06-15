# sat-solver

trying to write a sat solver, lets see how it goes

## how to download problems

Go to the `problems` folder then to whatever year you want and execute the following but change the uri file name to be whatever the one is for that folder.  This downloads a lot of data so give it some time.  The files should be in the same folder as the uri file as *.xz files and when you extract them they turn into *.cnf files.  All the downloaded and extracted files are both excluded in the gitignore so it's safe to download them and not worry about the repo.

```
wget --content-disposition -i track_main_2023.uri
```
