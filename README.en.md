# Effective testing of our HTTP APIs in .NET Core

When we decided to write about "Effective Testing of our HTTP APIs in .NET Core" we did not think about writing in depth about it. It is probably that with some blog entries we would have managed to share our ideas and extend a clear message across the readers. But in the end, the desire to share experiences and different solutions to common problems in our day to day, seems to take up a bit more than we thought. Therefore, we built this short manual, which we hope will serve to introduce you to the path of **functional testing** of our "HTTP APIs built with .NET Core 2.X".

## Introduction

You may have asked yourself, "Why functional tests on our HTTP API if I already have unit tests?" Well, the answer is quite simple, "because they are complementary". That is, having unit tests in our software developments does not prevent us from doing this kind of functional tests. Unit tests are fast and allow us to test different casuistics of our components in an isolated way.

On the contrary, the functional tests presented here are not so fast, but they allow us to test together the different components or parts that make up our solution and they end up beign a good indicator of code coverage.

Until recently, running these tests involved some work to make them be part of the normal development cycle of a programmer (coding, construction and tests). With the arrival of *TestServer* (available in both .NET Core and Full Framework with Web Api 2.X) this process has been simplified and we have the possibility to run this type of tests similarly to unit tests and therefore included them within the development flow that we usually follows.

## Content

The content of this manual is open and we will try to keep it as up-to-date as possible. Something that of course is not always easy for the delivery cycles of new features that we have in **.NET Core** project. In principle, we will be based on **.NET Core 2.X** specifically version **2.0**, the latest released version when this manual was being written.

## Table of Contents

1. [Introduction to TestServer](chapters/en/chapter1.md)
2. [Routes and parameters](chapters/en/chapter2.md)
3. [Authorization](chapters/en/chapter3.md)
4. [Working with data](chapters/en/chapter4.md)

Each **chapter** has the **code** associated with the explanation of it. You can find them in the **samples** folder. 

## Acknowledgements

To all the people who have contributed to the writing and revision of this manual, especially to:

1. Unai Zorrilla Castro
2. Luis Ruiz Pavon
