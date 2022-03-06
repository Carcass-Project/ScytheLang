# Scythe
Scythe is a systems programming language directed towards writing kernels and other low-level programs.

# Why did I make Scythe?
I felt bored, and I recently was using Rust and looking how to code an operating system using it.
I decided I'll make my own Rust and add in my own features.
It might not be the best idea but it's ok.

# Mockup

```
  package use Scythe::IO::Console
  
  fn example(a -> int32, b -> float32) : float32
  {
    return a+b;
  }
  
  fn main()
  {
     print(example(10, 5.4f)); // should return 15.4f.
  }
```
