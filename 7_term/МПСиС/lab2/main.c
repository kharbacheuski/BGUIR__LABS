#include <msp430.h>

#define LED1 BIT1  // P1.1 - светодиод
#define S1   BIT7  // P1.7 - кнопка S1
#define S2   BIT2  // P2.2 - кнопка S2

volatile unsigned int blink_delay = 500;  // Начальная задержка (в мс)

// Функция задержки
void delay_ms(unsigned int ms) {
    while (ms--) {
        __delay_cycles(1000);  // Задержка на 1 мс при тактовой частоте 1 МГц
    }
}

// Инициализация GPIO
void init_GPIO() {
    // Настраиваем светодиод как выход
    P1DIR |= LED1;
    P1OUT &= ~LED1;  // Светодиод выключен

    // Настраиваем кнопку S1 как вход с подтягивающим резистором
    P1DIR &= ~S1;    // S1 как вход
    P1REN |= S1;     // Включаем подтягивающий резистор
    P1OUT |= S1;     // Подтягиваем к Vcc

    // Включаем прерывание по спадающему фронту на S1 (нажатие кнопки)
    P1IE |= S1;
    P1IES |= S1;
    P1IFG &= ~S1;    // Очищаем флаг прерывания

    // Настраиваем кнопку S2 как вход с подтягивающим резистором
    P2DIR &= ~S2;    // S2 как вход
    P2REN |= S2;     // Включаем подтягивающий резистор
    P2OUT |= S2;     // Подтягиваем к Vcc

    // Включаем прерывание по спадающему фронту на S2 (нажатие кнопки)
    P2IE |= S2;
    P2IES |= S2;
    P2IFG &= ~S2;    // Очищаем флаг прерывания
}

// Главная функция
int main(void) {
    WDTCTL = WDTPW | WDTHOLD;  // Останавливаем сторожевой таймер

    init_GPIO();  // Инициализация GPIO

    __bis_SR_register(GIE);  // Включаем глобальные прерывания

    while (1) {
        P1OUT ^= LED1;  // Инвертируем состояние светодиода
        delay_ms(blink_delay);  // Задержка, зависящая от blink_delay
    }
}

// Прерывание от кнопки S1 (увеличение частоты мигания)
#pragma vector=PORT1_VECTOR
__interrupt void Port_1(void) {
    if (P1IFG & S1) {  // Если прерывание вызвано кнопкой S1
        if (blink_delay > 100) {
            blink_delay -= 100;  // Увеличиваем частоту (уменьшаем задержку)
        }
        P1IFG &= ~S1;  // Очищаем флаг прерывания
    }
}

// Прерывание от кнопки S2 (уменьшение частоты мигания)
#pragma vector=PORT2_VECTOR
__interrupt void Port_2(void) {
    if (P2IFG & S2) {  // Если прерывание вызвано кнопкой S2
        if (blink_delay < 1000) {
            blink_delay += 100;  // Уменьшаем частоту (увеличиваем задержку)
        }
        P2IFG &= ~S2;  // Очищаем флаг прерывания
    }
}
