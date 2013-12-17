HTTP Key Server
===============

This is a simple HTTP server implemented via .NET's HttpListener that
translates certain HTTP requests into simulated keystrokes by using
System.Windows.Forms.SendKey.

This was written to make it so that a smartphone could be used as a buzzer for
playing the trivia game "You Don't Know Jack 4". The included HTML interface is
designed for this purpose.


Security
--------

This provides the option for a password to be provided with the HTTP keystroke
requests. This provides enough security to keep your roommates from messing
with your game while you play, but not enough to keep an evil-doer on the
internet from taking over your computer, especially since the password is sent
in the clear.

Do NOT use a password that you would use for anything else.

Do NOT open the port that the server uses to anything other than your private
network.

Running this application lets anyone who can reach your computer on a network
essentially control your computer.

The application should be fine for use on a locked down, private LAN or
wireless network where you know and trust all of the people who can connect to
the network.


Basic Usage
-----------

Compile with csc, run the executable, and follow the instructions. Point your
phone's browser at your machine and type in the password you want to use. Once
the server is running, start your game, pick your buzzer key on your phone, and
play.

I recommend turning off auto-rotate on your phone, since the HTML interface
does not handle landscape mode well.


Modifications to the UI
-----------------------

If you need a different interface, e.g. for a different version of "You Don't
Know Jack", then just make your changes to index.html and restart the server.
