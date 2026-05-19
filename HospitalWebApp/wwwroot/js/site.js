// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundling and minify static web assets.

// Write your JavaScript code.

// Mobile menu toggle
function toggleMenu() {
    var x = document.getElementById("navLinks");
    var button = document.querySelector('.menu-toggle');
    var isOpen = x.classList.toggle("active");
    if (button) {
        button.setAttribute('aria-expanded', isOpen.toString());
    }
}

// Dropdown menu handling
document.addEventListener('DOMContentLoaded', function() {
    // Handle dropdown visibility on click for better mobile support
    const dropdownToggles = document.querySelectorAll('.dropdown-toggle');
    
    dropdownToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            // On mobile, toggle the dropdown
            if (window.innerWidth <= 768) {
                e.preventDefault();
                const container = this.closest('.dropdown-menu-container');
                const menu = container.querySelector('.dropdown-menu');
                
                // Close other menus
                document.querySelectorAll('.dropdown-menu-container').forEach(other => {
                    if (other !== container) {
                        other.classList.remove('open');
                    }
                });
                
                container.classList.toggle('open');
            }
        });
    });

    // Service card click handling - ensure cards are clickable
    const serviceCards = document.querySelectorAll('a[asp-controller="Services"]');
    serviceCards.forEach(card => {
        card.addEventListener('click', function(e) {
            // If this is a direct link, not a dropdown item
            if (!this.classList.contains('dropdown-toggle')) {
                window.location.href = this.href;
            }
        });
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', function(e) {
        if (window.innerWidth <= 768) {
            const dropdownContainers = document.querySelectorAll('.dropdown-menu-container');
            dropdownContainers.forEach(container => {
                if (!container.contains(e.target)) {
                    container.classList.remove('open');
                }
            });
        }
    });
});

