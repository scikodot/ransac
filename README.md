# RANSAC
C# implementation of the RANSAC algorithm.

**RANSAC** (**RAN**dom **SA**mple **C**onsensus) is an algorithm for measuring system parameters for some input data.<br>
A common task for this algorithm is to find the median subset of a specified set. For the regular spaces, the subset is:
- 2D space - a line
- 3D space - a plane

This implementation presents a search of a median plane for a random (or loaded) set of points in 3D space.
