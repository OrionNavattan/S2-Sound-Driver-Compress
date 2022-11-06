# S2 Sound Driver Compress
 Fork of AuroraFields' Dual PCM Compress modified to allow it to assemble Sonic 2.
 This has been modified to use .sax files extensions, to allow it it to pass on additional arguments 
 to the compressor, namely the -a flag to ClownLZSS saxman for accurate compression
 and the -S flag to Flamewing's saxcmp for sizeless compression, and most importantly,
 to allow it to patch the 68K Saxman decompressor with the size of the compressed Z80 binary.
